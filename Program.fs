open Discord
open Discord.Commands
open Discord.WebSocket
open DiscordBot.Services
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Models
open System.IO
open System.Threading.Tasks

let log (msg: LogMessage) =
    printfn "%s" <| msg.ToString()
    Task.CompletedTask

let configureServices (client: DiscordSocketClient) (config: IConfigurationRoot) (dbService: DatabaseService) =
    ServiceCollection()
        // Base
        .AddSingleton(
            client
        )
        .AddSingleton<CommandService>()
        .AddSingleton<CommandHandlingService>()
        // Logging
        .AddLogging(fun loggingBuilder -> loggingBuilder.AddConsole() |> ignore)
        // Extra
        .AddSingleton(
            config
        )
        // Add additional services here...
        .AddSingleton(
            dbService
        )
        .BuildServiceProvider()

let mainAsync =
    async {
        use client = new DiscordSocketClient()

        client.add_Log (fun msg -> log msg)

        let config =
            ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build()

        let dbService = DatabaseService(new SoberContext())

        let services =
            configureServices client config dbService

        do!
            services
                .GetRequiredService<CommandHandlingService>()
                .InitializeAsync(services)

        do!
            client.LoginAsync(TokenType.Bot, config.["token"])
            |> Async.AwaitTask

        do! client.StartAsync() |> Async.AwaitTask
        do! Task.Delay(-1) |> Async.AwaitTask
    }

[<EntryPoint>]
let main _ =
    mainAsync |> Async.RunSynchronously
    0
