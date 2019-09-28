using Discord.Commands;
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
                .Where(c => !string.IsNullOrEmpty(c.Summary))
                .Select(c => $"**{c.Name}{(c.Aliases.Any(a => a != c.Name) ? $" (Aliases: {string.Join(' ', c.Aliases.Where(a => a != c.Name).Select(a => $"{string.Join(", ", a)}"))})" : "")}{(c.Parameters.Any() ? " " : "")}{string.Join(' ', c.Parameters.Select(p => $"<{p.Name}>"))}** - {c.Summary}");
            return ReplyAsync(string.Join('\n', commandList));
        }
    }
}