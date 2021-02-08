namespace DiscordBot.Services

open Discord
open Discord.Commands
open Discord.WebSocket
open System
open System.Reflection
open System.Threading.Tasks

type CommandHandlingService
    (
        provider: IServiceProvider,
        discord: DiscordSocketClient,
        commands: CommandService,
        databaseService: DatabaseService
    ) =
    let _discord = discord
    let _commands = commands
    let _provider = provider
    let _databaseService = databaseService

    member _.MessageReceived(rawMessage: SocketMessage): Task =
        match rawMessage with
        | :? SocketUserMessage as message ->
            if (message.Source = MessageSource.User) then
                let argPos = 0

                let isCommand =
                    message.HasMentionPrefix(_discord.CurrentUser, ref argPos)

                let context = SocketCommandContext(_discord, message)
                _databaseService.UpdateActiveDate(context.Guild.Id, message.Author.Id)

                if not isCommand then
                    match _databaseService.GetNewMilestoneName(context.Guild.Id, message.Author.Id) with
                    | Some milestoneName ->
                        let milestoneChannel =
                            _databaseService.GetMilestoneChannel(context.Guild.Id)
                            |> Option.defaultValue 0UL

                        let milestoneMessage =
                            $"<@{message.Author.Id}> has reached a new milestone: **{milestoneName}**"

                        if (milestoneChannel > 0UL) then
                            context
                                .Guild
                                .GetTextChannel(milestoneChannel)
                                .SendMessageAsync(milestoneMessage)
                            |> Async.AwaitTask
                            |> Async.Ignore
                            |> Async.RunSynchronously
                        else
                            (context.Channel :?> SocketTextChannel)
                                .SendMessageAsync(milestoneMessage)
                            |> Async.AwaitTask
                            |> Async.Ignore
                            |> Async.RunSynchronously
                    | None -> ()

                if isCommand then
                    match _databaseService.GetBanMessage(context.Guild.Id, message.Author.Id) with
                    | Some banMessage ->
                        (context.Channel :?> SocketTextChannel)
                            .SendMessageAsync($"<@{message.Author.Id}> {banMessage}")
                        |> Async.AwaitTask
                        |> Async.Ignore
                        |> Async.RunSynchronously

                        Task.CompletedTask
                    | None ->
                        _databaseService.PruneInactiveUsers(context.Guild.Id)
                        |> ignore

                        // while (message.Content[argPos] == ' ') argPos++

                        let result =
                            _commands.ExecuteAsync(context, argPos, _provider)
                            |> Async.AwaitTask
                            |> Async.RunSynchronously

                        if result.Error = Nullable(CommandError.UnknownCommand) then
                            (context.Channel :?> SocketTextChannel)
                                .SendMessageAsync(
                                    text =
                                        sprintf
                                            "Unknown command: %s"
                                            (message.Content.Substring(message.Content.IndexOf('>') + 2))
                                )
                            |> Async.AwaitTask
                            |> Async.Ignore
                            |> Async.RunSynchronously

                            Task.CompletedTask
                        elif (result.Error.HasValue) then
                            (context.Channel :?> SocketTextChannel)
                                .SendMessageAsync(result.ToString())
                            |> Async.AwaitTask
                            |> Async.Ignore
                            |> Async.RunSynchronously

                            Task.CompletedTask
                        else
                            Task.CompletedTask
                else
                    Task.CompletedTask
            else
                Task.CompletedTask
        | _ -> Task.CompletedTask

    member this.InitializeAsync(provider) =
        async {
            let _provider = provider

            _commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider)
            |> ignore

            _discord.add_MessageReceived (fun message -> this.MessageReceived message)
        }
