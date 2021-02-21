namespace SoberBot.Modules

open Discord.Commands
open System
open System.Threading.Tasks

type UtilModule() =
    inherit ModuleBase<SocketCommandContext>()

    let buildCommandString (command: CommandInfo) =
        let aliases =
            String.Join(
                ' ',
                command.Aliases
                |> Seq.filter ((<>) command.Name)
                |> Seq.map (fun a -> sprintf "%s" (String.Join(", ", a)))
            )

        let aliasString =
            if command.Aliases |> Seq.contains command.Name then
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
            |> Seq.filter
                (fun c ->
                    not (String.IsNullOrEmpty(c.Summary))
                    && not (
                        c.Preconditions
                        |> Seq.exists (fun p -> p.Group = "Permission")
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

        this.ReplyAsync(null, false, embedBuilder.Build())
        |> Async.AwaitTask
        |> Async.Ignore
        |> Async.RunSynchronously

        let isAdmin =
            this.Context.User.Id = this.Context.Guild.OwnerId

        if isAdmin then
            let adminEmbedBuilder =
                Discord.EmbedBuilder().WithTitle("Admin Commands")

            let adminCommands =
                Services.commands.Commands
                |> Seq.filter
                    (fun c ->
                        not (String.IsNullOrEmpty(c.Summary))
                        && c.Preconditions
                           |> Seq.exists (fun p -> p.Group = "Permission"))

            for command in adminCommands do
                let embedFieldBuilder =
                    Discord
                        .EmbedFieldBuilder()
                        .WithIsInline(false)
                        .WithName(buildCommandString command)
                        .WithValue(command.Summary)

                adminEmbedBuilder.AddField(embedFieldBuilder)
                |> ignore

            this.ReplyAsync(null, false, adminEmbedBuilder.Build()) :> Task
        else
            Task.CompletedTask
