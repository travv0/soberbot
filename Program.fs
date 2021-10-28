module Program

open Discord
open System.Threading.Tasks

let mainAsync =
    async {
        do! CommandHandling.initializeAsync Services.provider

        do!
            Services.discord.LoginAsync(TokenType.Bot, Services.config.Token)
            |> Async.AwaitTask

        do! Services.discord.StartAsync() |> Async.AwaitTask
        do! Task.Delay(-1) |> Async.AwaitTask
    }

[<EntryPoint>]
let main _ =
    mainAsync |> Async.RunSynchronously
    0
