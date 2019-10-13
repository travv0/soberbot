using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class UtilModule : ModuleBase<SocketCommandContext>
    {
        private readonly CommandService _commandService;

        public UtilModule(CommandService commandService)
        {
            _commandService = commandService;
        }

        [Command("debug")]
        public Task Debug() => ReplyAsync($"Server ID: {Context.Guild.Id}\nUser ID: {Context.User.Id}");

        [Command("help")]
        [Summary("Shows this command list.")]
        public Task Help()
        {
            var commandList = _commandService.Commands
                .Where(c => !string.IsNullOrEmpty(c.Summary) && !c.Preconditions.Any(p => p.Group == "Permission"))
                .Select(c => $"**{c.Name}{(c.Aliases.Any(a => a != c.Name) ? $" (Aliases: {string.Join(' ', c.Aliases.Where(a => a != c.Name).Select(a => $"{string.Join(", ", a)}"))})" : "")}{(c.Parameters.Any() ? " " : "")}{string.Join(' ', c.Parameters.Select(p => $"<{p.Name}>"))}** - {c.Summary}");

            var isAdmin = Context.User.Id == Context.Guild.OwnerId;
            var adminCommandList = new List<string> { };
            if (isAdmin)
            {
                adminCommandList = _commandService.Commands
                    .Where(c => !string.IsNullOrEmpty(c.Summary) && c.Preconditions.Any(p => p.Group == "Permission"))
                    .Select(c => $"**{c.Name}{(c.Aliases.Any(a => a != c.Name) ? $" (Aliases: {string.Join(' ', c.Aliases.Where(a => a != c.Name).Select(a => $"{string.Join(", ", a)}"))})" : "")}{(c.Parameters.Any() ? " " : "")}{string.Join(' ', c.Parameters.Select(p => $"<{p.Name}>"))}** - {c.Summary}")
                    .ToList();
            }

            return ReplyAsync(string.Join('\n', commandList) + (isAdmin ? "\n\nAdmin Commands:\n" + string.Join('\n', adminCommandList) : ""));
        }
    }
}