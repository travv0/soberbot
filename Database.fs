module Database

open FSharpPlus
open Microsoft.EntityFrameworkCore
open Models
open System

let soberContext = new SoberContext()

let inline detach e =
    map
        (fun a ->
            soberContext.Entry(a).State <- EntityState.Detached
            a)
        e

let getServerSobrieties serverId =
    query {
        for sobriety in soberContext.Sobrieties do
            where (sobriety.ServerID = serverId)
    }
    |> toSeq
    |> detach

let getSobrieties serverId userId =
    query {
        for sobriety in soberContext.Sobrieties do
            where (
                sobriety.ServerID = serverId
                && sobriety.UserID = userId
            )
    }
    |> toSeq
    |> detach

let getSobriety serverId userId sobrietyType: Sobriety option =
    query {
        for sobriety in soberContext.Sobrieties do
            where (
                sobriety.ServerID = serverId
                && sobriety.UserID = userId
                && sobriety.Type = sobrietyType
            )
    }
    |> tryHead
    |> detach

let setDate serverId userId userName (soberDate: DateTime) sobrietyType =
    let lastMilestoneDays =
        (DateTime.Today - soberDate).TotalDays
        |> floor
        |> int

    match getSobriety serverId userId sobrietyType with
    | None ->
        soberContext.Sobrieties.Add(
            { ID = 0UL
              ServerID = serverId
              UserID = userId
              UserName = userName
              SobrietyDate = soberDate
              ActiveDate = DateTime.Today
              LastMilestoneDays = lastMilestoneDays
              MilestonesEnabled = true
              Type = sobrietyType }
        )
        |> ignore
    | Some existingRecord ->
        soberContext.Sobrieties.Update(
            { existingRecord with
                  SobrietyDate = soberDate
                  UserName = userName
                  LastMilestoneDays = lastMilestoneDays }
        )
        |> ignore

    soberContext.SaveChanges()

let removeSobriety serverId userId sobrietyType =
    match getSobriety serverId userId sobrietyType with
    | Some sobriety ->
        soberContext.Remove(sobriety) |> ignore
        soberContext.SaveChanges() |> ignore
    | None -> ()

let updateActiveDates serverId userId =
    match getSobrieties serverId userId |> toList with
    | [] -> ()
    | sobrieties ->
        for sobriety in sobrieties do
            soberContext.Update(
                { sobriety with
                      ActiveDate = DateTime.Now }
            )
            |> ignore

        soberContext.SaveChanges() |> ignore

let getConfig serverId: Config option =
    query {
        for config in soberContext.Config do
            where (config.ServerID = serverId)
    }
    |> tryHead
    |> detach

let pruneInactiveUsers serverId =
    let pruneDays =
        match getConfig serverId with
        | Some config -> config.PruneDays
        | None -> 30

    let inactiveSobrieties =
        getServerSobrieties serverId
        |> Seq.filter (fun s -> s.ActiveDate < DateTime.Now.AddDays(float (-pruneDays)))

    soberContext.Sobrieties.RemoveRange(inactiveSobrieties)
    soberContext.SaveChanges()

let setPruneDays serverId days =
    match getConfig serverId with
    | None ->
        soberContext.Config.Add(
            { ID = 0UL
              MilestoneChannelID = 0UL
              ServerID = serverId
              PruneDays = days }
        )
        |> ignore
    | Some config ->
        soberContext.Update({ config with PruneDays = days })
        |> ignore

    soberContext.SaveChanges()

let getBan serverId userId =
    query {
        for ban in soberContext.Bans do
            where (ban.ServerID = serverId && ban.UserID = userId)
    }
    |> tryHead
    |> detach

let banUser serverId userId message =
    match getBan serverId userId with
    | None ->
        soberContext.Bans.Add(
            { ID = 0UL
              ServerID = serverId
              UserID = userId
              Message = message }
        )
        |> ignore

        removeSobriety serverId userId |> ignore
    | Some ban ->
        soberContext.Update({ ban with Message = message })
        |> ignore

    soberContext.SaveChanges()

let unbanUser serverId userId =
    match getBan serverId userId with
    | Some ban ->
        soberContext.Remove(ban) |> ignore
        soberContext.SaveChanges() |> ignore
    | None -> ()

let getBanMessage serverId userId =
    getBan serverId userId
    |> map (fun ban -> ban.Message)

let getMilestones () =
    soberContext.Milestones |> toSeq |> detach

let getNewMilestoneNames serverId userId: (string * string) list =
    match getSobrieties serverId userId |> toList with
    | [] -> []
    | sobrieties ->
        [ for sobriety in sobrieties do
              if sobriety.MilestonesEnabled then
                  let milestone =
                      getMilestones ()
                      |> Seq.sortByDescending (fun m -> m.Days)
                      |> tryFind
                          (fun m ->
                              m.Days > sobriety.LastMilestoneDays
                              && (DateTime.Today - sobriety.SobrietyDate).TotalDays
                                 |> floor
                                 |> int
                                 >= m.Days)

                  match milestone with
                  | Some milestone ->
                      soberContext.Update(
                          { sobriety with
                                LastMilestoneDays = milestone.Days }
                      )
                      |> ignore

                      soberContext.SaveChanges() |> ignore
                      yield (sobriety.Type, milestone.Name)
                  | None -> () ]

let setMilestoneChannel serverId channelId =
    match getConfig serverId with
    | None ->
        soberContext.Config.Add(
            { ID = 0UL
              MilestoneChannelID = channelId
              ServerID = serverId
              PruneDays = 30 }
        )
        |> ignore
    | Some config ->
        soberContext.Update(
            { config with
                  MilestoneChannelID = channelId }
        )
        |> ignore

    soberContext.SaveChanges()

let getMilestoneChannel serverId =
    getConfig serverId
    |> Option.map (fun config -> config.MilestoneChannelID)

let enableMilestones serverId userId =
    match getSobrieties serverId userId |> toList with
    | [] -> ()
    | sobrieties ->
        for sobriety in sobrieties do
            soberContext.Update(
                { sobriety with
                      MilestonesEnabled = true }
            )
            |> ignore

        soberContext.SaveChanges() |> ignore

let disableMilestones serverId userId =
    match getSobrieties serverId userId |> toList with
    | [] -> ()
    | sobrieties ->
        for sobriety in sobrieties do
            soberContext.Update(
                { sobriety with
                      MilestonesEnabled = false }
            )
            |> ignore

        soberContext.SaveChanges() |> ignore
