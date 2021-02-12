namespace DiscordBot.Modules

open Discord.Commands
open FSharpPlus
open System
open System.Threading.Tasks

type UtilModule() =
    inherit ModuleBase<SocketCommandContext>()

    let buildCommandString (command: CommandInfo) =
        let aliases =
            String.Join(
                ' ',
                command.Aliases
                |> toSeq
                |> filter (fun a -> a <> command.Name)
                |> map (fun a -> sprintf "%s" (String.Join(", ", a)))
            )

        let aliasString =
            if command.Aliases
               |> exists (fun a -> a <> command.Name) then
                $" (Aliases: {aliases})"
            else
                ""

        let parameters =
            String.Join(
                ' ',
                command.Parameters
                |> Seq.map (fun p -> $"<{p.Name}>")
            )

        sprintf
            "%s%s%s%s"
            command.Name
            aliasString
            (if command.Parameters.Count > 0 then
                 " "
             else
                 "")
            parameters

    [<Command("help"); Summary("Shows this command list.")>]
    member this.Help(): Task =
        let commands =
            Services.commands.Commands
            |> filter
                (fun c ->
                    not (String.IsNullOrEmpty(c.Summary))
                    && not (
                        c.Preconditions
                        |> exists (fun p -> p.Group = "Permission")
                    ))

        let embedBuilder =
            Discord.EmbedBuilder().WithTitle("Commands")

        for command in commands do
            let embedFieldBuilder =
                Discord
                    .EmbedFieldBuilder()
                    .WithIsInline(false)
                    .WithName(buildCommandString command)
                    .WithValue(command.Summary)

            embedBuilder.AddField(embedFieldBuilder) |> ignore

        let isAdmin =
            this.Context.User.Id = this.Context.Guild.OwnerId

        if isAdmin then
            embedBuilder.WithTitle("Admin Commands") |> ignore

            let adminCommands =
                Services.commands.Commands
                |> filter
                    (fun c ->
                        not (String.IsNullOrEmpty(c.Summary))
                        && c.Preconditions
                           |> exists (fun p -> p.Group = "Permission"))

            for command in adminCommands do
                let embedFieldBuilder =
                    Discord
                        .EmbedFieldBuilder()
                        .WithIsInline(false)
                        .WithName(buildCommandString command)
                        .WithValue(command.Summary)

                embedBuilder.AddField(embedFieldBuilder) |> ignore

        embedBuilder
            .WithDescription("To use the bot, tag it and specify one of the commands shown below.  "
                             + "Replace the parts of commands surrounded by <> with your own text.\n"
                             + "**Example:** "
                             + Services.discord.CurrentUser.Mention
                             + " set 3/31/2020")
            .WithFooter(
                "Source code can be found at https://github.com/travv0/soberbot\n"
                + "Please report any bugs at https://github.com/travv0/soberbot/issues"
            )
        |> ignore

        this.ReplyAsync(null, false, embedBuilder.Build()) :> Task
