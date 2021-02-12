namespace DiscordBot.Services

open FSharpPlus
open Microsoft.EntityFrameworkCore
open Models
open System

type DatabaseService(soberContext: SoberContext) =
    member this.SetDate(serverId, userId, userName, soberDate: DateTime) =
        let lastMilestoneDays =
            (DateTime.Today - soberDate).TotalDays
            |> floor
            |> int

        match this.GetSobriety(serverId, userId) with
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

    member __.GetSobrieties(serverId) =
        soberContext.Sobrieties
        |> Seq.filter (fun s -> s.ServerID = serverId)

    member __.GetSobriety(serverId, userId): Sobriety option =
        soberContext.Sobrieties
        |> tryFind (fun s -> s.ServerID = serverId && s.UserID = userId)
        |> map
            (fun s ->
                soberContext.Entry(s).State <- EntityState.Detached
                s)

    member this.RemoveSobriety(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | Some sobriety ->
            soberContext.Remove(sobriety) |> ignore
            soberContext.SaveChanges() |> ignore
        | None -> ()

    member this.UpdateActiveDate(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | Some sobriety ->
            soberContext.Update(
                { sobriety with
                      ActiveDate = DateTime.Now }
            )
            |> ignore

            soberContext.SaveChanges() |> ignore
        | None -> ()

    member __.GetConfigs(serverId) =
        soberContext.Config
        |> Seq.filter (fun c -> c.ServerID = serverId)

    member this.PruneInactiveUsers(serverId) =
        let pruneDays =
            match this.GetConfig(serverId) with
            | Some config -> config.PruneDays
            | None -> 30

        let inactiveSobrieties =
            this.GetSobrieties(serverId)
            |> Seq.filter (fun s -> s.ActiveDate < DateTime.Now.AddDays(float (-pruneDays)))

        soberContext.Sobrieties.RemoveRange(inactiveSobrieties)
        soberContext.SaveChanges()

    member this.SetPruneDays(serverId, days) =
        match this.GetConfig(serverId) with
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

    member this.BanUser(serverId, userId, message) =
        match this.GetBan(serverId, userId) with
        | None ->
            soberContext.Bans.Add(
                { ID = 0UL
                  ServerID = serverId
                  UserID = userId
                  Message = message }
            )
            |> ignore

            this.RemoveSobriety(serverId, userId) |> ignore
        | Some ban ->
            soberContext.Update({ ban with Message = message })
            |> ignore

        soberContext.SaveChanges()

    member this.UnbanUser(serverId, userId) =
        match this.GetBan(serverId, userId) with
        | Some ban ->
            soberContext.Remove(ban) |> ignore
            soberContext.SaveChanges() |> ignore
        | None -> ()

    member __.GetBan(serverId, userId) =
        soberContext.Bans
        |> tryFind (fun b -> b.ServerID = serverId && b.UserID = userId)
        |> map
            (fun b ->
                soberContext.Entry(b).State <- EntityState.Detached
                b)

    member this.GetBanMessage(serverId, userId) =
        this.GetBan(serverId, userId)
        |> map (fun ban -> ban.Message)

    member __.GetMilestones() = soberContext.Milestones

    member this.GetNewMilestoneName(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | Some sobriety ->
            if sobriety.MilestonesEnabled then
                let milestone =
                    this.GetMilestones()
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

    member __.GetConfig(serverId): Config option =
        soberContext.Config
        |> tryFind (fun c -> c.ServerID = serverId)
        |> map
            (fun c ->
                soberContext.Entry(c).State <- EntityState.Detached
                c)

    member this.SetMilestoneChannel(serverId, channelId) =
        match this.GetConfig(serverId) with
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

    member this.GetMilestoneChannel(serverId) =
        this.GetConfig(serverId)
        |> Option.map (fun config -> config.MilestoneChannelID)

    member this.EnableMilestones(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | None -> ()
        | Some sobriety ->
            soberContext.Update(
                { sobriety with
                      MilestonesEnabled = true }
            )
            |> ignore

            soberContext.SaveChanges() |> ignore

    member this.DisableMilestones(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | None -> ()
        | Some sobriety ->
            soberContext.Update(
                { sobriety with
                      MilestonesEnabled = false }
            )
            |> ignore

            soberContext.SaveChanges() |> ignore
