open System.IO
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Discord
open Discord.Commands
open Discord.WebSocket
open DiscordBot.Services
open Models

let configureServices (client: DiscordSocketClient) (config: IConfigurationRoot) (dbService: DatabaseService) =
    ServiceCollection()
        // Base
        .AddSingleton(
            client
        )
        .AddSingleton<CommandService>()
        .AddSingleton<CommandHandlingService>()
        // Logging
        .AddLogging()
        .AddSingleton<LogService>()
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

        let config =
            ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json")
                .Build()

        let dbService = DatabaseService(new SoberContext())

        let services =
            configureServices client config dbService

        services.GetRequiredService<LogService>()
        |> ignore

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
