namespace DiscordBot.Modules

open Discord
open Discord.Commands
open DiscordBot.Services
open Models
open System
open System.Linq
open System.Threading.Tasks

type SobrietyModule(databaseService: DatabaseService) =
    inherit ModuleBase<SocketCommandContext>()

    let _databaseService = databaseService

    [<Command("set")>]
    [<Summary("Sets your sobriety date to a date in the MM/DD/YYYY format.")>]
    member this.Set(dateString) = this.Set(dateString, this.Context.User)

    [<Command("set")>]
    [<RequireUserPermission(GuildPermission.Administrator, Group = "Permission")>]
    [<RequireOwner(Group = "Permission")>]
    [<Summary("Sets a given user's sobriety date to a date in the MM/DD/YYYY format.")>]
    member this.Set(dateString, user) =
        try
            let soberDate = DateTime.Parse(dateString)

            _databaseService.SetDate(this.Context.Guild.Id, user.Id, user.Username, soberDate)
            |> ignore

            this.ReplyAsync($"Sober date set to {soberDate.ToShortDateString()} for {user.Username}")
        with _ -> this.ReplyAsync($"Please enter date in MM/DD/YYYY format")

    [<Command("reset")>]
    [<Alias("set")>]
    [<Summary("Resets your sobriety date to today.  Because of timezones, this might be different than the date where you are.")>]
    member this.Reset() = this.Reset(this.Context.User)

    [<Command("reset")>]
    [<RequireUserPermission(GuildPermission.Administrator, Group = "Permission")>]
    [<RequireOwner(Group = "Permission")>]
    [<Summary("Resets a given user's sobriety date to today.")>]
    member this.Reset(user) =
        let today = DateTime.Today

        _databaseService.SetDate(this.Context.Guild.Id, user.Id, user.Username, today)
        |> ignore

        this.ReplyAsync($"Sober date reset to {today.ToShortDateString()} for {user.Username}")

    member this.List(orderBy: Func<Sobriety, IComparable>, numbered: bool) =
        let today = DateTime.Today

        let sobrieties =
            _databaseService
                .GetSobrieties(this.Context.Guild.Id)
                .OrderBy(orderBy)

        let list =
            sobrieties
            |> Seq.mapi
                (fun i s ->
                    let soberDays =
                        Math.Floor((today - s.SobrietyDate).TotalDays)
                        |> int

                    let number = if numbered then $"{i + 1}. " else ""
                    sprintf "%s%s - %d day%s sober" number s.UserName soberDays (if soberDays = 1 then "" else "s"))

        this.ReplyAsync(String.Join('\n', list))

    [<Command("list")>]
    [<Summary("Lists all users on the server ordered by user name and how many days of sobriety they have.")>]
    member this.List() =
        this.List((fun s -> s.UserName :> IComparable), false)

    [<Command("leaderboard")>]
    [<Summary("Lists all users on the server ordered by sober time and how many days of sobriety they have.")>]
    member this.Leaderboard() =
        this.List((fun s -> s.SobrietyDate :> IComparable), true)

    member this.Days(user: IUser, isSelf) =
        let today = DateTime.Today

        match _databaseService.GetSobriety(this.Context.Guild.Id, user.Id) with
        | None ->
            this.ReplyAsync(
                $"No date set for {user.Username}."
                + (if isSelf then
                       "  Use the set or reset command to set your start date."
                   else
                       "")
            )
        | Some sobriety ->
            let soberDays =
                Math.Floor((today - sobriety.SobrietyDate).TotalDays)
                |> int

            this.ReplyAsync(
                sprintf "%s - %d day%s sober" sobriety.UserName soberDays (if soberDays = 1 then "" else "s")
            )

    [<Command("days")>]
    [<Summary("Shows how many days of sobriety you have.")>]
    member this.Days() = this.Days(this.Context.User, true)

    [<Command("days")>]
    [<Summary("Shows how many days of sobriety a given user has.")>]
    member this.Days(user) = this.Days(user, false)

    member this.Delete(user: IUser, isSelf) =
        _databaseService.RemoveSobriety(this.Context.Guild.Id, user.Id)

        this.ReplyAsync(
            $"{user.Username} has been removed from the database."
            + (if isSelf then
                   "  Sorry to see you go :("
               else
                   "")
        )

    [<Command("break")>]
    [<Alias("delete")>]
    [<Summary("Take a break from sobriety and remove yourself from the database. :(")>]
    member this.Delete() = this.Delete(this.Context.User, true)

    [<Command("break")>]
    [<Alias("delete")>]
    [<RequireUserPermission(GuildPermission.Administrator, Group = "Permission")>]
    [<RequireOwner(Group = "Permission")>]
    [<Summary("Remove a given user from the database.")>]
    member this.Delete(user) = this.Delete(user, false)

    [<Command("milestones on")>]
    [<Summary("Enable milestone notifications.")>]
    member this.MilestonesOn() =
        _databaseService.EnableMilestones(this.Context.Guild.Id, this.Context.User.Id)
        this.ReplyAsync($"Milestones enabled for {this.Context.User.Username}.")

    [<Command("milestones off")>]
    [<Summary("Disable milestone notifications.")>]
    member this.MilestonesOff() =
        _databaseService.DisableMilestones(this.Context.Guild.Id, this.Context.User.Id)
        this.ReplyAsync($"Milestones disabled for {this.Context.User.Username}.")

    [<Command("config prunedays")>]
    [<RequireUserPermission(GuildPermission.Administrator, Group = "Permission")>]
    [<RequireOwner(Group = "Permission")>]
    [<Summary("Sets the number of days of inactivity before users are pruned from the database.")>]
    member this.ConfigPrunedays(days) =
        _databaseService.SetPruneDays(this.Context.Guild.Id, days)
        |> ignore

        this.ReplyAsync($"Users will now be removed from database after {days} days of inactivity.")

    [<Command("config milestonechannel")>]
    [<RequireUserPermission(GuildPermission.Administrator, Group = "Permission")>]
    [<RequireOwner(Group = "Permission")>]
    [<Summary("Sets the channel that milestone notifications are sent to.  By default, notifications will be sent to last channel user is active in.")>]
    member this.ConfigMilestonechannel(channel: IChannel) =
        _databaseService.SetMilestoneChannel(this.Context.Guild.Id, channel.Id)
        |> ignore

        this.ReplyAsync($"Milestones will be posted to <#{channel.Id}>")

    [<Command("config unset milestonechannel")>]
    [<RequireUserPermission(GuildPermission.Administrator, Group = "Permission")>]
    [<RequireOwner(Group = "Permission")>]
    [<Summary("Sets milestone notifications to be sent to last channel user is active in.")>]
    member this.ConfigUnsetMilestonechannel() =
        _databaseService.SetMilestoneChannel(this.Context.Guild.Id, 0UL)
        |> ignore

        this.ReplyAsync($"Milestones will be posted to last channel user posted in.")

    [<Command("ban")>]
    [<RequireUserPermission(GuildPermission.Administrator, Group = "Permission")>]
    [<RequireOwner(Group = "Permission")>]
    [<Summary("Removes user from database and no longer allows them to user the bot.  Replies with given message instead.")>]
    member this.Ban(user: IUser, message) =
        _databaseService.BanUser(this.Context.Guild.Id, user.Id, message)
        |> ignore

        this.ReplyAsync($"{user.Username} banned with message: {message}")

    [<Command("unban")>]
    [<RequireUserPermission(GuildPermission.Administrator, Group = "Permission")>]
    [<RequireOwner(Group = "Permission")>]
    [<Summary("Allow banned user to use the bot once again.")>]
    member this.Unban(user: IUser) =
        _databaseService.UnbanUser(this.Context.Guild.Id, user.Id)
        this.ReplyAsync($"{user.Username} has been unbanned.")
