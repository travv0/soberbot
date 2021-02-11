namespace DiscordBot.Services

open System
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Discord
open Discord.Commands
open Discord.WebSocket

type LogService(discord: DiscordSocketClient, commands: CommandService) =
    let discordLogger = loggerFactory.CreateLogger("discord")

    let commandsLogger = loggerFactory.CreateLogger("commands")

    member this.LogDiscord(message: LogMessage) =
        this.discordLogger.Log(
            this.LogLevelFromSeverity(message.Severity),
            null,
            message,
            message.Exception,
            fun (_1, _2) -> message.ToString(prependTimestamp = false)
        )


        Task.CompletedTask

    member this.Discord =
        discord.add_Log (fun m -> this.LogDiscord m)
        discord

    member this.Commands =
        commands.add_Log (fun m -> this.LogCommand m)
        commands

    member this.LogCommand(message) =
        // Return an error message for async commands
        match message.Exception with
        | :? CommandException as command ->
            // Don't risk blocking the logging task by awaiting a message send ratelimits!?
            command.Context.Channel.SendMessageAsync($"Error: {command.Message}")
            |> ignore
        | _ -> ()

        this.commandsLogger.Log(
            this.LogLevelFromSeverity(message.Severity),
            null,
            message,
            message.Exception,
            fun (_1, _2) -> message.ToString(prependTimestamp = false)
        )

        Task.CompletedTask

    member _.LogLevelFromSeverity(severity) =
        enum<LogLevel> (Math.Abs((int) severity - 5))
