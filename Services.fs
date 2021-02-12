module Services

open Discord
open Discord.Commands
open Discord.WebSocket
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System.IO
open System.Threading.Tasks
open System
open System.Text.Json

type Config = { Token: string }

let config =
    let options =
        JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

    let json = File.ReadAllText("config.json")
    JsonSerializer.Deserialize<Config>(json, options)

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
