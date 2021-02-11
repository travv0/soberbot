﻿namespace DiscordBot.Services

open Models
open System
open FSharpPlus

type DatabaseService(soberContext: SoberContext) =
    member this.SetDate(serverId, userId, userName, soberDate) =
        match this.GetSobriety(serverId, userId) with
        | None ->
            let sobriety =
                { ID = 0UL
                  ServerID = serverId
                  UserID = userId
                  UserName = userName
                  SobrietyDate = soberDate
                  ActiveDate = DateTime.Today
                  LastMilestoneDays = int (Math.Floor((DateTime.Today - soberDate).TotalDays))
                  MilestonesEnabled = true }

            soberContext.Sobrieties.Add(sobriety) |> ignore
        | Some existingRecord ->

            soberContext.Sobrieties.Update(
                { existingRecord with
                      SobrietyDate = soberDate
                      UserName = userName
                      LastMilestoneDays = int (Math.Floor((DateTime.Today - soberDate).TotalDays)) }
            )
            |> ignore

        soberContext.SaveChanges()

    member __.GetSobrieties(serverId) =
        soberContext.Sobrieties
        |> Seq.filter (fun s -> s.ServerID = serverId)
        |> toList

    member __.GetSobriety(serverId, userId): Sobriety option =
        soberContext.Sobrieties
        |> tryFind (fun s -> s.ServerID = serverId && s.UserID = userId)

    member this.RemoveSobriety(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | Some sobriety ->
            soberContext. |> ignore
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

    member __.PruneInactiveUsers(serverId) =
        let pruneDays =
            match soberContext.Config
                  |> Seq.filter (fun c -> c.ServerID = serverId)
                  |> toList with
            | (config :: _) -> config.PruneDays
            | _ -> 30

        let inactiveSobrieties =
            soberContext.Sobrieties
            |> Seq.filter
                (fun s ->
                    s.ServerID = serverId
                    && s.ActiveDate < DateTime.Now.AddDays(float (-pruneDays)))

        soberContext.Sobrieties.RemoveRange(inactiveSobrieties)
        soberContext.SaveChanges()

    member __.SetPruneDays(serverId, days) =
        match soberContext.Config
              |> tryFind (fun c -> c.ServerID = serverId) with
        | None ->
            let config = Config()
            config.ServerID <- serverId
            config.PruneDays <- days
            soberContext.Config.Add(config) |> ignore
        | Some config ->
            config.PruneDays <- days
            soberContext.Update(config) |> ignore

        soberContext.SaveChanges()

    member this.BanUser(serverId, userId, message) =
        match soberContext.Bans
              |> tryFind (fun b -> b.ServerID = serverId && b.UserID = userId) with
        | None ->
            let ban = Ban()
            ban.ServerID <- serverId
            ban.UserID <- userId
            ban.Message <- message
            soberContext.Bans.Add(ban) |> ignore
            this.RemoveSobriety(serverId, userId) |> ignore
        | Some ban ->
            ban.Message <- message
            soberContext.Update(ban) |> ignore

        soberContext.SaveChanges()

    member __.UnbanUser(serverId, userId) =
        match soberContext.Bans
              |> tryFind (fun b -> b.ServerID = serverId && b.UserID = userId) with
        | Some ban ->
            soberContext.Remove(ban) |> ignore
            soberContext.SaveChanges() |> ignore
        | None -> ()

    member __.GetBanMessage(serverId, userId) =
        soberContext.Bans
        |> tryFind (fun b -> b.ServerID = serverId && b.UserID = userId)
        |> Option.map (fun ban -> ban.Message)

    member this.GetNewMilestoneName(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | Some sobriety ->
            if sobriety.MilestonesEnabled then
                let milestone =
                    soberContext.Milestones
                    |> Seq.sortByDescending (fun m -> m.Days)
                    |> tryFind
                        (fun m ->
                            m.Days > sobriety.LastMilestoneDays
                            && (int << round) (DateTime.Today - sobriety.SobrietyDate).TotalDays
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

    member __.SetMilestoneChannel(serverId, channelId) =
        match soberContext.Config
              |> tryFind (fun c -> c.ServerID = serverId) with
        | None ->
            let config = Config()
            config.ServerID <- serverId
            config.MilestoneChannelID <- channelId
            soberContext.Config.Add(config) |> ignore
        | Some config ->
            config.MilestoneChannelID <- channelId
            soberContext.Update(config) |> ignore

        soberContext.SaveChanges()

    member __.GetMilestoneChannel(serverId) =
        soberContext.Config
        |> tryFind (fun c -> c.ServerID = serverId)
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
