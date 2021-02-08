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
        async {
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

                    if isCommand then
                        match _databaseService.GetBanMessage(context.Guild.Id, message.Author.Id) with
                        | Some banMessage ->
                            do!
                                (context.Channel :?> SocketTextChannel)
                                    .SendMessageAsync($"<@{message.Author.Id}> {banMessage}")
                                |> Async.AwaitTask
                                |> Async.Ignore
                        | None ->
                            _databaseService.PruneInactiveUsers(context.Guild.Id)
                            |> ignore

                            // while (message.Content[argPos] == ' ') argPos++

                            let! result =
                                _commands.ExecuteAsync(context, argPos, _provider)
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
            let _provider = provider

            _commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider)
            |> ignore

            _discord.add_MessageReceived (fun message -> this.MessageReceived message)
        }
