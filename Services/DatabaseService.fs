namespace DiscordBot.Services

open Models
open System

type DatabaseService(soberContext: SoberContext) =
    let _context =
        soberContext.Database.EnsureCreated() |> ignore
        soberContext

    member this.SetDate(serverId, userId, userName, soberDate) =
        match this.GetSobriety(serverId, userId) with
        | None ->
            let sobriety = Sobriety()
            sobriety.ServerID <- serverId
            sobriety.UserID <- userId
            sobriety.UserName <- userName
            sobriety.SobrietyDate <- soberDate
            sobriety.ActiveDate <- DateTime.Today
            sobriety.LastMilestoneDays <- int (Math.Floor((DateTime.Today - soberDate).TotalDays))
            sobriety.MilestonesEnabled <- true

            _context.Sobrieties.Add(sobriety) |> ignore
        | Some existingRecord ->
            existingRecord.SobrietyDate <- soberDate
            existingRecord.UserName <- userName
            existingRecord.LastMilestoneDays <- int (Math.Floor((DateTime.Today - soberDate).TotalDays))

            _context.Sobrieties.Update(existingRecord)
            |> ignore

        _context.SaveChanges()

    member __.GetSobrieties(serverId) =
        _context.Sobrieties
        |> Seq.filter (fun s -> s.ServerID = serverId)
        |> Seq.toList

    member __.GetSobriety(serverId, userId): Sobriety option =
        let r =
            _context.Sobrieties
            |> Seq.tryFind (fun s -> s.ServerID = serverId && s.UserID = userId)

        printfn "HERE4"
        r

    member this.RemoveSobriety(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | Some sobriety ->
            _context.Remove(sobriety) |> ignore
            _context.SaveChanges() |> ignore
        | None -> ()

    member this.UpdateActiveDate(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | Some sobriety ->
            sobriety.ActiveDate <- DateTime.Now
            _context.Update(sobriety) |> ignore
            _context.SaveChanges() |> ignore
        | None -> printfn "HERE3"

    member __.PruneInactiveUsers(serverId) =
        let pruneDays =
            match _context.Config
                  |> Seq.filter (fun c -> c.ServerID = serverId)
                  |> Seq.toList with
            | (config :: _) -> config.PruneDays
            | _ -> 30

        let inactiveSobrieties =
            _context.Sobrieties
            |> Seq.filter
                (fun s ->
                    s.ServerID = serverId
                    && s.ActiveDate < DateTime.Now.AddDays(float (-pruneDays)))

        _context.RemoveRange(inactiveSobrieties)
        _context.SaveChanges()

    member __.SetPruneDays(serverId, days) =
        match _context.Config
              |> Seq.tryFind (fun c -> c.ServerID = serverId) with
        | None ->
            let config = Config()
            config.ServerID <- serverId
            config.PruneDays <- days
            _context.Config.Add(config) |> ignore
        | Some config ->
            config.PruneDays <- days
            _context.Update(config) |> ignore

        _context.SaveChanges()

    member this.BanUser(serverId, userId, message) =
        match _context.Bans
              |> Seq.tryFind (fun b -> b.ServerID = serverId && b.UserID = userId) with
        | None ->
            let ban = Ban()
            ban.ServerID <- serverId
            ban.UserID <- userId
            ban.Message <- message
            _context.Bans.Add(ban) |> ignore
            this.RemoveSobriety(serverId, userId) |> ignore
        | Some ban ->
            ban.Message <- message
            _context.Update(ban) |> ignore

        _context.SaveChanges()

    member __.UnbanUser(serverId, userId) =
        match _context.Bans
              |> Seq.tryFind (fun b -> b.ServerID = serverId && b.UserID = userId) with
        | Some ban ->
            _context.Remove(ban) |> ignore
            _context.SaveChanges() |> ignore
        | None -> ()

    member __.GetBanMessage(serverId, userId) =
        _context.Bans
        |> Seq.tryFind (fun b -> b.ServerID = serverId && b.UserID = userId)
        |> Option.map (fun ban -> ban.Message)

    member this.GetNewMilestoneName(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | Some sobriety ->
            if sobriety.MilestonesEnabled then
                let milestone =
                    _context.Milestones
                    |> Seq.sortByDescending (fun m -> m.Days)
                    |> Seq.tryFind
                        (fun m ->
                            m.Days > sobriety.LastMilestoneDays
                            && (int << round) (DateTime.Today - sobriety.SobrietyDate).TotalDays
                               >= m.Days)

                match milestone with
                | Some milestone ->
                    sobriety.LastMilestoneDays <- milestone.Days
                    _context.Update(sobriety) |> ignore
                    _context.SaveChanges() |> ignore
                    Some(milestone.Name)
                | None -> None
            else
                None
        | _ -> None

    member __.SetMilestoneChannel(serverId, channelId) =
        match _context.Config
              |> Seq.tryFind (fun c -> c.ServerID = serverId) with
        | None ->
            let config = Config()
            config.ServerID <- serverId
            config.MilestoneChannelID <- channelId
            _context.Config.Add(config) |> ignore
        | Some config ->
            config.MilestoneChannelID <- channelId
            _context.Update(config) |> ignore

        _context.SaveChanges()

    member __.GetMilestoneChannel(serverId) =
        _context.Config
        |> Seq.tryFind (fun c -> c.ServerID = serverId)
        |> Option.map (fun config -> config.MilestoneChannelID)

    member this.EnableMilestones(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | None -> ()
        | Some sobriety ->
            sobriety.MilestonesEnabled <- true
            _context.Update(sobriety) |> ignore
            _context.SaveChanges() |> ignore

    member this.DisableMilestones(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | None -> ()
        | Some sobriety ->
            sobriety.MilestonesEnabled <- false
            _context.Update(sobriety) |> ignore
            _context.SaveChanges() |> ignore
