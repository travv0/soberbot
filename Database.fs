namespace DiscordBot.Services

open FSharpPlus
open Microsoft.EntityFrameworkCore
open Models
open System

module Database =
    let soberContext = new SoberContext()

    let getSobrieties serverId =
        soberContext.Sobrieties
        |> Seq.filter (fun s -> s.ServerID = serverId)

    let getSobriety serverId userId: Sobriety option =
        soberContext.Sobrieties
        |> tryFind (fun s -> s.ServerID = serverId && s.UserID = userId)
        |> map
            (fun s ->
                soberContext.Entry(s).State <- EntityState.Detached
                s)

    let setDate serverId userId userName (soberDate: DateTime) =
        let lastMilestoneDays =
            (DateTime.Today - soberDate).TotalDays
            |> floor
            |> int

        match getSobriety serverId userId with
        | None ->
            soberContext.Sobrieties.Add(
                { ID = 0UL
                  ServerID = serverId
                  UserID = userId
                  UserName = userName
                  SobrietyDate = soberDate
                  ActiveDate = DateTime.Today
                  LastMilestoneDays = lastMilestoneDays
                  MilestonesEnabled = true }
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

    let removeSobriety serverId userId =
        match getSobriety serverId userId with
        | Some sobriety ->
            soberContext.Remove(sobriety) |> ignore
            soberContext.SaveChanges() |> ignore
        | None -> ()

    let updateActiveDate serverId userId =
        match getSobriety serverId userId with
        | Some sobriety ->
            soberContext.Update(
                { sobriety with
                      ActiveDate = DateTime.Now }
            )
            |> ignore

            soberContext.SaveChanges() |> ignore
        | None -> ()

    let getConfigs serverId =
        soberContext.Config
        |> Seq.filter (fun c -> c.ServerID = serverId)

    let getConfig serverId: Config option =
        soberContext.Config
        |> tryFind (fun c -> c.ServerID = serverId)
        |> map
            (fun c ->
                soberContext.Entry(c).State <- EntityState.Detached
                c)

    let pruneInactiveUsers serverId =
        let pruneDays =
            match getConfig serverId with
            | Some config -> config.PruneDays
            | None -> 30

        let inactiveSobrieties =
            getSobrieties serverId
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
        soberContext.Bans
        |> tryFind (fun b -> b.ServerID = serverId && b.UserID = userId)
        |> map
            (fun b ->
                soberContext.Entry(b).State <- EntityState.Detached
                b)

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

    let getMilestones () = soberContext.Milestones

    let getNewMilestoneName serverId userId =
        match getSobriety serverId userId with
        | Some sobriety ->
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
                    Some(milestone.Name)
                | None -> None
            else
                None
        | _ -> None

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
        match getSobriety serverId userId with
        | None -> ()
        | Some sobriety ->
            soberContext.Update(
                { sobriety with
                      MilestonesEnabled = true }
            )
            |> ignore

            soberContext.SaveChanges() |> ignore

    let disableMilestones serverId userId =
        match getSobriety serverId userId with
        | None -> ()
        | Some sobriety ->
            soberContext.Update(
                { sobriety with
                      MilestonesEnabled = false }
            )
            |> ignore

            soberContext.SaveChanges() |> ignore
