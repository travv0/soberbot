namespace DiscordBot.Services

open Models
open System
open System.Collections.Generic
open System.Linq
open System.Text
open System.Threading.Tasks

type DatabaseService() =
    let mutable _context = new SoberContext()

    member _.Initialize(soberContext) =
        _context <- soberContext
        _context.Database.EnsureCreated()

    member this.SetDate(serverId, userId, userName, soberDate) =
        match this.GetSobriety(serverId, userId) with
        | None ->
            _context.Sobrieties.Add(
                { ID = 0UL
                  ServerID = serverId
                  UserID = userId
                  UserName = userName
                  SobrietyDate = soberDate
                  ActiveDate = DateTime.Today
                  LastMilestoneDays = int (Math.Floor((DateTime.Today - soberDate).TotalDays))
                  MilestonesEnabled = true }: Sobriety
            )
            |> ignore
        | Some existingRecord ->
            _context.Sobrieties.Update(
                { existingRecord with
                      SobrietyDate = soberDate
                      UserName = userName
                      LastMilestoneDays = int (Math.Floor((DateTime.Today - soberDate).TotalDays)) }
            )
            |> ignore

        _context.SaveChanges()

    member _.GetSobrieties(serverId) =
        _context
            .Sobrieties
            .Where(fun s -> s.ServerID = serverId)
            .ToList()

    member _.GetSobriety(serverId, userId): Sobriety option =
        _context.Sobrieties
        |> Seq.where (fun s -> s.ServerID = serverId && s.UserID = userId)
        |> Seq.tryHead

    member this.RemoveSobriety(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | Some sobriety ->
            _context.Remove(sobriety) |> ignore
            _context.SaveChanges() |> ignore
        | None -> ()

    member this.UpdateActiveDate(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | Some sobriety ->
            _context.Update(
                { sobriety with
                      ActiveDate = DateTime.Now }
            )
            |> ignore

            _context.SaveChanges() |> ignore
        | None -> ()

    member _.PruneInactiveUsers(serverId) =
        let pruneDays =
            match _context.Config.Where(fun c -> c.ServerID = serverId)
                  |> Seq.toList with
            | (config :: _) -> config.PruneDays
            | _ -> 30

        let inactiveSobrieties =
            _context.Sobrieties.Where
                (fun s ->
                    s.ServerID = serverId
                    && s.ActiveDate < DateTime.Now.AddDays(float (-pruneDays)))

        _context.RemoveRange(inactiveSobrieties)
        _context.SaveChanges()

    member _.SetPruneDays(serverId, days) =
        match _context.Config.Where(fun c -> c.ServerID = serverId)
              |> Seq.tryHead with
        | None ->
            _context.Config.Add(
                { ID = 0UL
                  MilestoneChannelID = 0UL
                  ServerID = serverId
                  PruneDays = days }
            )
            |> ignore
        | Some config ->
            _context.Update({ config with PruneDays = days })
            |> ignore

        _context.SaveChanges()

    member this.BanUser(serverId, userId, message) =
        match _context.Bans.Where(fun b -> b.ServerID = serverId && b.UserID = userId)
              |> Seq.tryHead with
        | None ->
            _context.Bans.Add(
                { ID = 0UL
                  ServerID = serverId
                  UserID = userId
                  Message = message }
            )
            |> ignore

            this.RemoveSobriety(serverId, userId) |> ignore
        | Some ban ->
            _context.Update({ ban with Message = message })
            |> ignore

        _context.SaveChanges()

    member _.UnbanUser(serverId, userId) =
        match _context.Bans.Where(fun b -> b.ServerID = serverId && b.UserID = userId)
              |> Seq.tryHead with
        | Some ban ->
            _context.Remove(ban) |> ignore
            _context.SaveChanges() |> ignore
        | None -> ()

    member _.GetBanMessage(serverId, userId) =
        _context.Bans.Where(fun b -> b.ServerID = serverId && b.UserID = userId)
        |> Seq.tryHead
        |> Option.map (fun ban -> ban.Message)

    member this.GetNewMilestoneName(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | Some sobriety ->
            if sobriety.MilestonesEnabled then
                let milestone =
                    _context
                        .Milestones
                        .OrderByDescending(fun m -> m.Days)
                        .Where(fun m ->
                            m.Days > sobriety.LastMilestoneDays
                            && (int << round) (DateTime.Today - sobriety.SobrietyDate).TotalDays
                               >= m.Days)
                    |> Seq.tryHead

                match milestone with
                | Some milestone ->
                    _context.Update(
                        { sobriety with
                              LastMilestoneDays = milestone.Days }
                    )
                    |> ignore

                    _context.SaveChanges() |> ignore

                    Some(milestone.Name)
                | None -> None
            else
                None
        | _ -> None

    member _.SetMilestoneChannel(serverId, channelId) =
        match _context.Config.Where(fun c -> c.ServerID = serverId)
              |> Seq.tryHead with
        | None ->
            _context.Config.Add(
                { ID = 0UL
                  PruneDays = 0
                  ServerID = serverId
                  MilestoneChannelID = channelId }
            )
            |> ignore
        | Some config ->
            _context.Update(
                { config with
                      MilestoneChannelID = channelId }
            )
            |> ignore

        _context.SaveChanges()

    member _.GetMilestoneChannel(serverId) =
        _context.Config.Where(fun c -> c.ServerID = serverId)
        |> Seq.tryHead
        |> Option.map (fun config -> config.MilestoneChannelID)

    member this.EnableMilestones(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | None -> ()
        | Some sobriety ->
            _context.Update(
                { sobriety with
                      MilestonesEnabled = true }
            )
            |> ignore

            _context.SaveChanges() |> ignore

    member this.DisableMilestones(serverId, userId) =
        match this.GetSobriety(serverId, userId) with
        | None -> ()
        | Some sobriety ->
            _context.Update(
                { sobriety with
                      MilestonesEnabled = false }
            )
            |> ignore

            _context.SaveChanges() |> ignore
