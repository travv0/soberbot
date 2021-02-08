namespace DiscordBot.Modules

open Discord.Commands
open Discord.WebSocket
open System.Collections.Generic
open System
open System.Linq
open System.Threading.Tasks

type UtilModule(commandService: CommandService, discord: DiscordSocketClient) =
    inherit ModuleBase<SocketCommandContext>()

    member val _commandService = commandService
    member val _discord = discord

    member _.BuildCommandString(command: CommandInfo) =
        let aliases =
            String.Join(
                ' ',
                command
                    .Aliases
                    .Where(fun a -> a <> command.Name)
                    .Select(fun a -> sprintf "%s" (String.Join(", ", a)))
            )

        let aliasString =
            if command.Aliases.Any(fun a -> a <> command.Name) then
                $" (Aliases: {aliases})"
            else
                ""

        let parameters =
            String.Join(' ', command.Parameters.Select(fun p -> $"<{p.Name}>"))

        sprintf
            "%s%s%s%s"
            command.Name
            aliasString
            (if command.Parameters.Any() then
                 " "
             else
                 "")
            parameters

    [<Command("help")>]
    [<Summary("Shows this command list.")>]
    member this.Help() =
        let commands =
            this._commandService.Commands.Where
                (fun c ->
                    not (String.IsNullOrEmpty(c.Summary))
                    && not (c.Preconditions.Any(fun p -> p.Group = "Permission")))

        let embedBuilder = Discord.EmbedBuilder()

        embedBuilder.WithTitle("Commands") |> ignore

        for command in commands do
            let embedFieldBuilder =
                Discord
                    .EmbedFieldBuilder()
                    .WithIsInline(false)
                    .WithName(this.BuildCommandString(command))
                    .WithValue(command.Summary)

            embedBuilder.AddField(embedFieldBuilder) |> ignore

        let isAdmin =
            this.Context.User.Id = this.Context.Guild.OwnerId

        if isAdmin then
            embedBuilder.WithTitle("Admin Commands") |> ignore

            let adminCommands =
                this._commandService.Commands.Where
                    (fun c ->
                        not (String.IsNullOrEmpty(c.Summary))
                        && c.Preconditions.Any(fun p -> p.Group = "Permission"))

            for command in adminCommands do
                let embedFieldBuilder =
                    Discord
                        .EmbedFieldBuilder()
                        .WithIsInline(false)
                        .WithName(this.BuildCommandString(command))
                        .WithValue(command.Summary)

                embedBuilder.AddField(embedFieldBuilder) |> ignore

        embedBuilder
            .WithDescription("To use the bot, tag it and specify one of the commands shown below.  "
                             + "Replace the parts of commands surrounded by <> with your own text.\n"
                             + "**Example:** "
                             + this._discord.CurrentUser.Mention
                             + " set 3/31/2020")
            .WithFooter(
                "Source code can be found at https://github.com/travv0/soberbot\n"
                + "Please report any bugs at https://github.com/travv0/soberbot/issues"
            )
        |> ignore

        this.ReplyAsync(null, false, embedBuilder.Build())
