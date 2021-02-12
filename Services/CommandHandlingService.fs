namespace DiscordBot.Services

open Discord
open Discord.Commands
open Discord.WebSocket
open System
open System.Reflection
open System.Threading.Tasks

type CommandHandlingService(provider: IServiceProvider, discord: DiscordSocketClient, commands: CommandService) =
    let log (msg: LogMessage) =
        // Return an error message for async commands
        match msg.Exception with
        | :? CommandException as command ->
            // Don't risk blocking the logging task by awaiting a message send ratelimits!?
            command.Context.Channel.SendMessageAsync($"Error: {command.Message}")
            |> ignore
        | _ -> ()

        printfn "%s" <| msg.ToString()
        Task.CompletedTask

    member __.MessageReceived(rawMessage: SocketMessage): Task =
        async {
            match rawMessage with
            | :? SocketUserMessage as message ->
                if (message.Source = MessageSource.User) then
                    let mutable argPos = 0

                    let isCommand =
                        message.HasMentionPrefix(discord.CurrentUser, &argPos)

                    let context = SocketCommandContext(discord, message)
                    Database.updateActiveDate context.Guild.Id message.Author.Id

                    if not isCommand then
                        match Database.getNewMilestoneName context.Guild.Id message.Author.Id with
                        | Some milestoneName ->
                            let milestoneChannel =
                                Database.getMilestoneChannel context.Guild.Id
                                |> Option.defaultValue 0UL

                            let milestoneMessage =
                                $"<@{message.Author.Id}> has reached a new milestone: **{milestoneName}**"

                            if (milestoneChannel > 0UL) then
                                do!
                                    context
                                        .Guild
                                        .GetTextChannel(milestoneChannel)
                                        .SendMessageAsync(milestoneMessage)
                                    |> Async.AwaitTask
                                    |> Async.Ignore
                            else
                                do!
                                    (context.Channel :?> SocketTextChannel)
                                        .SendMessageAsync(milestoneMessage)
                                    |> Async.AwaitTask
                                    |> Async.Ignore
                        | None -> ()
                    else
                        match Database.getBanMessage context.Guild.Id message.Author.Id with
                        | Some banMessage ->
                            do!
                                (context.Channel :?> SocketTextChannel)
                                    .SendMessageAsync($"<@{message.Author.Id}> {banMessage}")
                                |> Async.AwaitTask
                                |> Async.Ignore
                        | None ->
                            Database.pruneInactiveUsers context.Guild.Id
                            |> ignore

                            while (message.Content.[argPos] = ' ') do
                                argPos <- argPos + 1

                            let! result =
                                commands.ExecuteAsync(context, argPos, provider)
                                |> Async.AwaitTask

                            if result.Error = Nullable(CommandError.UnknownCommand) then
                                do!
                                    (context.Channel :?> SocketTextChannel)
                                        .SendMessageAsync(
                                            text =
                                                sprintf
                                                    "Unknown command: %s"
                                                    (message.Content.Substring(message.Content.IndexOf('>') + 2))
                                        )
                                    |> Async.AwaitTask
                                    |> Async.Ignore
                            elif (result.Error.HasValue) then
                                do!
                                    (context.Channel :?> SocketTextChannel)
                                        .SendMessageAsync(result.ToString())
                                    |> Async.AwaitTask
                                    |> Async.Ignore
            | _ -> ()
        }
        |> Async.RunSynchronously

        Task.CompletedTask

    member this.InitializeAsync(provider) =
        async {
            do!
                commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider)
                |> Async.AwaitTask
                |> Async.Ignore

            discord.add_MessageReceived (fun message -> this.MessageReceived message)
            commands.add_Log (fun msg -> log msg)
        }
