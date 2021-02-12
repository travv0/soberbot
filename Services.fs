module Services

open Discord
open Discord.Commands
open Discord.WebSocket
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System.IO
open System.Threading.Tasks

let config =
    ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("config.json")
        .Build()

let discord =
    let log (msg: LogMessage) =
        printfn "%s" <| msg.ToString()
        Task.CompletedTask

    let client = new DiscordSocketClient()
    client.add_Log (fun msg -> log msg)
    client

let commands = new CommandService()

let provider =
    ServiceCollection()
        .AddLogging(fun loggingBuilder -> loggingBuilder.AddConsole() |> ignore)
        .BuildServiceProvider()
