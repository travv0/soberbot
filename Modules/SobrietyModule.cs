using System;
using System.Threading.Tasks;
using Discord.Commands;
using DiscordBot.Models;
using DiscordBot.Services;

namespace DiscordBot.Modules
{
    public class SobrietyModule : ModuleBase<SocketCommandContext>
    {
        private readonly DatabaseService _databaseService;

        public SobrietyModule(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        [Command("set")]
        public Task Set()
        {
            return ReplyAsync($"set");
        }

        [Command("reset")]
        public Task Reset()
        {
            var today = DateTime.Today;
            _databaseService.SetDate(Context.Guild.Id, Context.User.Id, today);
            return ReplyAsync($"Sober date reset to {today.ToShortDateString()} for {Context.User.Username}");
        }

        [Command("list")]
        public Task List()
        {
            return ReplyAsync($"list");
        }

        [Command("break")]
        public Task Break()
        {
            return ReplyAsync($"break");
        }
    }
}
