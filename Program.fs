module Program

open Discord
open System.Threading.Tasks

[<EntryPoint>]
let main _ =
    let task =
        task {
            do! CommandHandling.initialize Services.provider
            do! Services.discord.LoginAsync(TokenType.Bot, Services.config.Token)
            do! Services.discord.StartAsync()
            do! Task.Delay(-1)
        }

    task.Result

    0
