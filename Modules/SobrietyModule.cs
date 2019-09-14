using Discord;
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
        [Summary("Sets your sobriety date to a date in the MM/DD/YYYY format.")]
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
        [Alias("set")]
        [Summary("Resets your sobriety date to today.  Because of timezones, this might be different than the date where you are.")]
        public Task Reset()
        {
            var today = DateTime.Today;
            _databaseService.SetDate(Context.Guild.Id, Context.User.Id, Context.User.Username, today);
            return ReplyAsync($"Sober date reset to {today.ToShortDateString()} for {Context.User.Username}");
        }

        [Command("list")]
        [Summary("Lists all users on the server ordered by user name and how many days of sobriety they have.")]
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

        [Command("leaderboard")]
        [Summary("Lists all users on the server ordered by sober time and how many days of sobriety they have.")]
        public Task Leaderboard()
        {
            var today = DateTime.Today;
            var sobrieties = _databaseService.GetSobrieties(Context.Guild.Id).OrderBy(s => s.SobrietyDate);
            var list = sobrieties.Select((s, i) =>
            {
                var soberDays = Math.Floor((today - s.SobrietyDate).TotalDays);
                return $"{i + 1}. {s.UserName} - {soberDays} day{(soberDays == 1 ? "" : "s")} sober";
            });
            return ReplyAsync(string.Join('\n', list));
        }

        [Command("days")]
        [Summary("Shows how many days of sobriety you have.")]
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

        [Command("days")]
        [Summary("Shows how many days of sobriety a given user has.")]
        public Task Days(IUser user)
        {
            var today = DateTime.Today;
            var sobriety = _databaseService.GetSobriety(Context.Guild.Id, user.Id);
            if (sobriety == null)
            {
                return ReplyAsync($"No date set for {user.Username}.");
            }
            else
            {
                var soberDays = Math.Floor((today - sobriety.SobrietyDate).TotalDays);
                return ReplyAsync($"{sobriety.UserName} - {soberDays} day{(soberDays == 1 ? "" : "s")} sober");
            }
        }

        [Command("break")]
        [Summary("Take a break from sobriety and remove yourself from the database. :(")]
        public Task Break()
        {
            _databaseService.RemoveSobriety(Context.Guild.Id, Context.User.Id);
            return ReplyAsync($"{Context.User.Username} has been removed from the database.  Sorry to see you go :(");
        }
    }
}