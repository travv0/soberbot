module CommandHandling

open Discord
open Discord.Commands
open Discord.WebSocket
open System
open System.Reflection
open System.Threading.Tasks

let log (msg: LogMessage) =
    // Return an error message for async commands
    match msg.Exception with
    | :? CommandException as command ->
        // Don't risk blocking the logging task by awaiting a message send rate limits!?
        command.Context.Channel.SendMessageAsync($"Error: {command.Message}")
        |> ignore
    | _ -> ()

    printfn "%s" <| msg.ToString()
    Task.CompletedTask

let messageReceived (rawMessage: SocketMessage) : Task =
    task {
        match rawMessage with
        | :? SocketUserMessage as message ->
            if (message.Source = MessageSource.User) then
                let mutable argPos = 0

                let isCommand =
                    message.HasMentionPrefix(Services.discord.CurrentUser, &argPos)

                let context =
                    SocketCommandContext(Services.discord, message)

                Database.updateActiveDates context.Guild.Id message.Author.Id

                if not isCommand then
                    match Database.getNewMilestoneNames context.Guild.Id message.Author.Id with
                    | [] -> ()
                    | milestoneNames ->
                        let milestoneChannel =
                            Database.getMilestoneChannel context.Guild.Id
                            |> Option.defaultValue 0UL

                        for sobrietyType, milestoneName in milestoneNames do
                            let sobrietyTypeMessage =
                                match sobrietyType with
                                | "" -> ""
                                | sobrietyType -> sprintf " for %s" sobrietyType

                            let milestoneMessage =
                                sprintf
                                    "<@%d> has reached a new milestone%s: **%s**"
                                    message.Author.Id
                                    sobrietyTypeMessage
                                    milestoneName

                            if (milestoneChannel > 0UL) then
                                do!
                                    context
                                        .Guild
                                        .GetTextChannel(milestoneChannel)
                                        .SendMessageAsync(milestoneMessage)
                                    :> Task
                            else
                                do!
                                    (context.Channel :?> SocketTextChannel)
                                        .SendMessageAsync(milestoneMessage)
                                    :> Task
                else
                    match Database.getBanMessage context.Guild.Id message.Author.Id with
                    | Some banMessage ->
                        do!
                            (context.Channel :?> SocketTextChannel)
                                .SendMessageAsync($"<@{message.Author.Id}> {banMessage}")
                            :> Task
                    | None ->
                        Database.pruneInactiveUsers context.Guild.Id
                        |> ignore

                        while (message.Content.[argPos] = ' ') do
                            argPos <- argPos + 1

                        let! result = Services.commands.ExecuteAsync(context, argPos, Services.provider)

                        if result.Error = Nullable(CommandError.UnknownCommand) then
                            do!
                                (context.Channel :?> SocketTextChannel)
                                    .SendMessageAsync(
                                        text =
                                            sprintf
                                                "Unknown command: %s"
                                                (message.Content.Substring(message.Content.IndexOf('>') + 2))
                                    )
                                :> Task
                        elif result.Error.HasValue then
                            do!
                                (context.Channel :?> SocketTextChannel)
                                    .SendMessageAsync(result.ToString())
                                :> Task
        | _ -> ()
    }

let initialize provider =
    task {
        do! Services.commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider) :> Task

        Func<_, _>(messageReceived)
        |> Services.discord.add_MessageReceived

        Func<_, _>(log) |> Services.commands.add_Log
    }
