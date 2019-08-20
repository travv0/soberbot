using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordBot.Modules
{
    public class DebugModule : ModuleBase<SocketCommandContext>
    {
        [Command("debug")]
        public Task Debug() => ReplyAsync($"Server ID: {Context.Guild.Id}\nUser ID: {Context.User.Id}");
    }
}
