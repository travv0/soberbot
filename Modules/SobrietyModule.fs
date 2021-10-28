namespace SoberBot.Modules

open Discord
open Discord.Commands
open Models
open System
open System.Threading.Tasks

type SobrietyModule() =
    inherit ModuleBase<SocketCommandContext>()

    [<Command("set"); Summary("Sets your sobriety date to a date in the MM/DD/YYYY format.")>]
    member this.Set(dateString) : Task =
        this.Set("", dateString, this.Context.User)

    [<Command("set");
      RequireUserPermission(GuildPermission.Administrator, Group = "Permission");
      RequireOwner(Group = "Permission");
      Summary("Sets a given user's sobriety date to a date in the MM/DD/YYYY format.")>]
    member this.Set(dateString, user) : Task = this.Set("", dateString, user)

    [<Command("set"); Summary("Sets your sobriety date for the given addiction to a date in the MM/DD/YYYY format.")>]
    member this.Set(addiction, dateString) : Task =
        this.Set(addiction, dateString, this.Context.User)

    [<Command("set");
      RequireUserPermission(GuildPermission.Administrator, Group = "Permission");
      RequireOwner(Group = "Permission");
      Summary("Sets a given user's sobriety date for given addiction to a date in the MM/DD/YYYY format.")>]
    member this.Set(addiction: string, dateString, user) : Task =
        try
            let soberDate = DateTime.Parse(dateString)

            Database.setDate this.Context.Guild.Id user.Id user.Username soberDate addiction
            |> ignore

            let sobrietyTypeMessage =
                match addiction with
                | "" -> ""
                | sobrietyType -> sprintf " for %s" sobrietyType

            this.ReplyAsync(
                $"Sober date set to {soberDate.ToShortDateString()}{sobrietyTypeMessage} for {user.Username}"
            )
        with
        | _ -> this.ReplyAsync($"Please enter date in MM/DD/YYYY format")
        :> Task

    [<Command("reset");
      Summary("Resets your sobriety date to today.  Because of timezones, this might be different than the date where you are.")>]
    member this.Reset() : Task = this.Reset(this.Context.User :> IUser)

    [<Command("reset");
      RequireUserPermission(GuildPermission.Administrator, Group = "Permission");
      RequireOwner(Group = "Permission");
      Summary("Resets a given user's sobriety date to today.")>]
    member this.Reset(user: IUser) : Task = this.Reset("", user)

    [<Command("reset");
      Summary("Resets your sobriety date to today for the given addiction.  Because of timezones, this might be different than the date where you are.")>]
    member this.Reset(addiction) : Task =
        this.Reset(addiction, this.Context.User)

    [<Command("reset");
      RequireUserPermission(GuildPermission.Administrator, Group = "Permission");
      RequireOwner(Group = "Permission");
      Summary("Resets a given user's sobriety date to today for the given addiction.")>]
    member this.Reset(addiction, user: IUser) : Task =
        let today = DateTime.Today

        Database.setDate this.Context.Guild.Id user.Id user.Username today addiction
        |> ignore

        let sobrietyTypeMessage =
            match addiction with
            | "" -> ""
            | sobrietyType -> sprintf " for %s" sobrietyType

        this.ReplyAsync($"Sober date reset to {today.ToShortDateString()}{sobrietyTypeMessage} for {user.Username}")
        :> Task

    member this.Reply(message) =
        this.ReplyAsync(message)
        |> Async.AwaitTask
        |> Async.Ignore
        |> Async.RunSynchronously

    member this.List(orderBy: Sobriety -> #IComparable, numbered: bool) =
        let today = DateTime.Today

        let sobrieties =
            query {
                for sobriety in Database.getServerSobrieties this.Context.Guild.Id do
                    sortBy (orderBy sobriety)
            }

        let list =
            sobrieties
            |> Seq.mapi (fun i s ->
                let soberDays =
                    Math.Floor((today - s.SobrietyDate).TotalDays)
                    |> int

                let sobrietyTypeMessage =
                    match s.Type with
                    | "" -> ""
                    | sobrietyType -> sprintf " from %s" sobrietyType

                let number = if numbered then $"{i + 1}. " else ""

                sprintf
                    "%s%s - %d day%s sober%s"
                    number
                    s.UserName
                    soberDays
                    (if soberDays = 1 then "" else "s")
                    sobrietyTypeMessage)

        match List.ofSeq list with
        | o :: list ->
            let mutable output = o

            for line in list do
                if String.length output + String.length line > 2000 then
                    this.Reply(output)
                    output <- line
                else
                    output <- output + "\n" + line

            this.ReplyAsync(output) :> Task
        | [] -> Task.CompletedTask

    [<Command("list");
      Summary("Lists all users on the server ordered by user name and how many days of sobriety they have.")>]
    member this.List() : Task = this.List((fun s -> s.UserName), false)

    [<Command("leaderboard");
      Summary("Lists all users on the server ordered by sober time and how many days of sobriety they have.")>]
    member this.Leaderboard() : Task =
        this.List((fun s -> s.SobrietyDate), true)

    member this.Days(user: IUser, isSelf, sobrietyType) =
        let today = DateTime.Today

        match Database.getSobriety this.Context.Guild.Id user.Id sobrietyType with
        | None -> this.SendNoDateMessage(user, isSelf)
        | Some sobriety ->
            let soberDays =
                Math.Floor((today - sobriety.SobrietyDate).TotalDays)
                |> int

            let sobrietyTypeMessage =
                match sobriety.Type with
                | "" -> ""
                | sobrietyType -> sprintf " from %s" sobrietyType

            this.ReplyAsync(
                sprintf
                    "%s - %d day%s sober%s"
                    sobriety.UserName
                    soberDays
                    (if soberDays = 1 then "" else "s")
                    sobrietyTypeMessage
            )
        :> Task

    [<Command("days"); Summary("Shows how many days of sobriety you have.")>]
    member this.Days() : Task = this.Days(this.Context.User, true, "")

    [<Command("days"); Summary("Shows how many days of sobriety a given user has.")>]
    member this.Days(user) : Task = this.Days(user, false, "")

    [<Command("days"); Summary("Shows how many days of sobriety you have for the given addiction.")>]
    member this.Days(addiction) : Task =
        this.Days(this.Context.User, true, addiction)

    [<Command("days"); Summary("Shows how many days of sobriety a given user has for the given addiction.")>]
    member this.Days(addiction: string, user: IUser) : Task = this.Days(user, false, addiction)

    member this.SendNoDateMessage(user: IUser, isSelf) =
        this.ReplyAsync(
            $"No date set for {user.Username}."
            + (if isSelf then
                   "  Use the set or reset command to set your start date."
               else
                   "")
        )

    member this.Date(user: IUser, isSelf, sobrietyType) =
        match Database.getSobriety this.Context.Guild.Id user.Id sobrietyType with
        | None -> this.SendNoDateMessage(user, isSelf)
        | Some sobriety ->
            let sobrietyTypeMessage =
                match sobrietyType with
                | "" -> ""
                | sobrietyType -> sprintf " from %s" sobrietyType

            this.ReplyAsync(
                $"{user.Username} has been sober{sobrietyTypeMessage} since {sobriety.SobrietyDate.ToShortDateString()}"
            )
        :> Task

    [<Command("date"); Summary("Shows your sobriety date.")>]
    member this.Date() : Task = this.Date(this.Context.User, true, "")

    [<Command("date"); Summary("Shows a given user's sobriety date.")>]
    member this.Date(user: IUser) : Task = this.Date(user, false, "")

    [<Command("date"); Summary("Shows your sobriety date for the given addiction.")>]
    member this.Date(addiction) : Task =
        this.Date(this.Context.User, true, addiction)

    [<Command("date"); Summary("Shows a given user's sobriety date for the given addiction.")>]
    member this.Date(addiction, user: IUser) : Task = this.Date(user, false, addiction)

    member this.Delete(user: IUser, isSelf, sobrietyType) =
        Database.removeSobriety this.Context.Guild.Id user.Id sobrietyType

        let sobrietyTypeMessage =
            match sobrietyType with
            | "" -> ""
            | sobrietyType -> sprintf " for %s" sobrietyType

        this.ReplyAsync(
            $"{user.Username} has been removed from the database{sobrietyTypeMessage}."
            + (if isSelf then
                   "  Sorry to see you go :("
               else
                   "")
        )
        :> Task

    [<Command("break");
      Alias("delete");
      Summary("Take a break from sobriety and remove yourself from the database. :(")>]
    member this.Delete() : Task =
        this.Delete(this.Context.User, true, "")

    [<Command("break");
      Alias("delete");
      RequireUserPermission(GuildPermission.Administrator, Group = "Permission");
      RequireOwner(Group = "Permission");
      Summary("Remove a given user from the database.")>]
    member this.Delete(user) : Task = this.Delete(user, false, "")

    [<Command("break");
      Alias("delete");
      Summary("Take a break from sobriety from the given addiction and remove yourself from the database. :(")>]
    member this.Delete(addiction) : Task =
        this.Delete(this.Context.User, true, addiction)

    [<Command("break");
      Alias("delete");
      RequireUserPermission(GuildPermission.Administrator, Group = "Permission");
      RequireOwner(Group = "Permission");
      Summary("Remove a given user from the database for the given addiction.")>]
    member this.Delete(addiction, user) : Task = this.Delete(user, false, addiction)

    [<Command("milestones on"); Summary("Enable milestone notifications.")>]
    member this.MilestonesOn() : Task =
        Database.enableMilestones this.Context.Guild.Id this.Context.User.Id
        this.ReplyAsync($"Milestones enabled for {this.Context.User.Username}.") :> Task

    [<Command("milestones off"); Summary("Disable milestone notifications.")>]
    member this.MilestonesOff() : Task =
        Database.disableMilestones this.Context.Guild.Id this.Context.User.Id
        this.ReplyAsync($"Milestones disabled for {this.Context.User.Username}.") :> Task

    [<Command("config prunedays");
      RequireUserPermission(GuildPermission.Administrator, Group = "Permission");
      RequireOwner(Group = "Permission");
      Summary("Sets the number of days of inactivity before users are pruned from the database.")>]
    member this.ConfigPrunedays(days) : Task =
        Database.setPruneDays this.Context.Guild.Id days
        |> ignore

        this.ReplyAsync($"Users will now be removed from database after {days} days of inactivity.") :> Task

    [<Command("config milestonechannel");
      RequireUserPermission(GuildPermission.Administrator, Group = "Permission");
      RequireOwner(Group = "Permission");
      Summary("Sets the channel that milestone notifications are sent to.  By default, notifications will be sent to last channel user is active in.")>]
    member this.ConfigMilestonechannel(channel: IChannel) : Task =
        Database.setMilestoneChannel this.Context.Guild.Id channel.Id
        |> ignore

        this.ReplyAsync($"Milestones will be posted to <#{channel.Id}>") :> Task

    [<Command("config unset milestonechannel");
      RequireUserPermission(GuildPermission.Administrator, Group = "Permission");
      RequireOwner(Group = "Permission");
      Summary("Sets milestone notifications to be sent to last channel user is active in.")>]
    member this.ConfigUnsetMilestonechannel() : Task =
        Database.setMilestoneChannel this.Context.Guild.Id 0UL
        |> ignore

        this.ReplyAsync($"Milestones will be posted to last channel user posted in.") :> Task

    [<Command("ban");
      RequireUserPermission(GuildPermission.Administrator, Group = "Permission");
      RequireOwner(Group = "Permission");
      Summary("Removes user from database and no longer allows them to user the bot.  Replies with given message instead.")>]
    member this.Ban(user: IUser, message) : Task =
        Database.banUser this.Context.Guild.Id user.Id message
        |> ignore

        this.ReplyAsync($"{user.Username} banned with message: {message}") :> Task

    [<Command("unban");
      RequireUserPermission(GuildPermission.Administrator, Group = "Permission");
      RequireOwner(Group = "Permission");
      Summary("Allow banned user to use the bot once again.")>]
    member this.Unban(user: IUser) : Task =
        Database.unbanUser this.Context.Guild.Id user.Id
        this.ReplyAsync($"{user.Username} has been unbanned.") :> Task
