using Discord.Commands;
using DiscordBot.Models;
using DiscordBot.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

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
        public Task Set(string dateString)
        {
            try
            {
                var soberDate = DateTime.Parse(dateString);
                _databaseService.SetDate(Context.Guild.Id, Context.User.Id, Context.User.Username, soberDate);
                return ReplyAsync($"Sober date set to {soberDate.ToShortDateString()} for {Context.User.Username}");
            }
            catch
            {
                return ReplyAsync($"Please enter date in MM/DD/YYYY format");
            }
        }

        [Command("reset")]
        public Task Reset()
        {
            var today = DateTime.Today;
            _databaseService.SetDate(Context.Guild.Id, Context.User.Id, Context.User.Username, today);
            return ReplyAsync($"Sober date reset to {today.ToShortDateString()} for {Context.User.Username}");
        }

        [Command("list")]
        public Task List()
        {
            var today = DateTime.Today;
            var sobrieties = _databaseService.GetSobrieties(Context.Guild.Id).OrderBy(s => s.UserName);
            var list = sobrieties.Select(s =>
            {
                var soberDays = Math.Floor((today - s.SobrietyDate).TotalDays);
                return $"{s.UserName} - {soberDays} day{(soberDays == 1 ? "" : "s")} sober";
            });
            return ReplyAsync(string.Join('\n', list));
        }

        [Command("days")]
        public Task Days()
        {
            var today = DateTime.Today;
            var sobriety = _databaseService.GetSobriety(Context.Guild.Id, Context.User.Id);
            if (sobriety == null)
            {
                return ReplyAsync($"No date set for {Context.User.Username}.  Use the set or reset command to set your start date.");
            }
            else
            {
                var soberDays = Math.Floor((today - sobriety.SobrietyDate).TotalDays);
                return ReplyAsync($"{sobriety.UserName} - {soberDays} day{(soberDays == 1 ? "" : "s")} sober");
            }
        }
    }
}