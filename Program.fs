open System.IO
open System.Threading.Tasks
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Discord
open Discord.Commands
open Discord.WebSocket
open DiscordBot.Services
open Models

let configureServices (client: IDiscordClient) (config: IConfigurationRoot) =
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
        .AddSingleton<DatabaseService>()
        .BuildServiceProvider()

let mainAsync () =
    use client = new DiscordSocketClient()

    let config =
        ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("config.json")
            .Build()

    let services = configureServices client config

    services.GetRequiredService<LogService>()
    |> ignore

    services
        .GetRequiredService<CommandHandlingService>()
        .InitializeAsync(services)
    |> Async.RunSynchronously

    services
        .GetRequiredService<DatabaseService>()
        .Initialize(new SoberContext())
    |> ignore

    client.LoginAsync(TokenType.Bot, config.["token"])
    |> Async.AwaitTask
    |> Async.RunSynchronously

    client.StartAsync()
    |> Async.AwaitTask
    |> Async.RunSynchronously

    Task.Delay(-1)
    |> Async.AwaitTask
    |> Async.RunSynchronously

[<EntryPoint>]
let main _ =
    mainAsync ()
    0
