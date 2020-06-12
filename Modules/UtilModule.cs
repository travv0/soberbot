using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class UtilModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commandService;
        private readonly DiscordSocketClient _discord;

        public UtilModule(CommandService commandService, DiscordSocketClient discord)
        {
            _commandService = commandService;
            _discord = discord;
        }

        string BuildCommandString(CommandInfo command)
        {
            var aliases = string.Join(' ', command.Aliases
                .Where(a => a != command.Name)
                .Select(a => $"{string.Join(", ", a)}"));
            var aliasString = command.Aliases.Any(a => a != command.Name) ? $" (Aliases: {aliases})" : "";
            var parameters = string.Join(' ', command.Parameters.Select(p => $"<{p.Name}>"));
            return $"{command.Name}{aliasString}{(command.Parameters.Any() ? " " : "")}{parameters}";
        }

        [Command("help")]
        [Summary("Shows this command list.")]
        public Task Help()
        {
            var commands = _commandService.Commands
                .Where(c => !string.IsNullOrEmpty(c.Summary) && !c.Preconditions.Any(p => p.Group == "Permission"));

            var embedBuilder = new Discord.EmbedBuilder();
            embedBuilder.WithTitle("Commands");
            foreach (var command in commands)
            {
                var embedFieldBuilder = new Discord.EmbedFieldBuilder()
                    .WithIsInline(false)
                    .WithName(BuildCommandString(command))
                    .WithValue(command.Summary);
                embedBuilder.AddField(embedFieldBuilder);
            }

            var isAdmin = Context.User.Id == Context.Guild.OwnerId;
            if (isAdmin)
            {
                embedBuilder.WithTitle("Admin Commands");
                var adminCommands = _commandService.Commands
                    .Where(c => !string.IsNullOrEmpty(c.Summary)
                             && c.Preconditions.Any(p => p.Group == "Permission"));
                foreach (var command in adminCommands)
                {
                    var embedFieldBuilder = new Discord.EmbedFieldBuilder()
                        .WithIsInline(false)
                        .WithName(BuildCommandString(command))
                        .WithValue(command.Summary);
                    embedBuilder.AddField(embedFieldBuilder);
                }
            }

            embedBuilder.WithDescription("To use the bot, tag it and specify one of the commands shown below.  "
                                       + "Replace the parts of commands surrounded by <> with your own text.\n"
                                       + "Example: " + _discord.CurrentUser.Mention + " set 2/31/2020")
                .WithFooter("Source code can be found at https://github.com/travv0/soberbot\n"
                          + "Please report any bugs at https://github.com/travv0/soberbot/issues");

            return ReplyAsync(null, false, embedBuilder.Build());
        }
    }
}