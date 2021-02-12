module Services

open Discord
open Discord.Commands
open Discord.WebSocket
open FSharp.Data
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System.IO
open System.Threading.Tasks
open System

type Config = JsonProvider<"""{"token": "token"}""">

let config =
    File.ReadAllText("config.json") |> Config.Parse

let discord =
    let log (msg: LogMessage) =
        printfn "%s" <| msg.ToString()
        Task.CompletedTask

    let client = new DiscordSocketClient()
    Func<_, _>(log) |> client.add_Log
    client

let commands = new CommandService()

let provider =
    ServiceCollection()
        .AddLogging(fun loggingBuilder -> loggingBuilder.AddConsole() |> ignore)
        .BuildServiceProvider()
