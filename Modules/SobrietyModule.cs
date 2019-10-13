using Discord;
using Discord.Commands;
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

        [Command("set")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [Summary("Sets a given user's sobriety date to a date in the MM/DD/YYYY format.")]
        public Task Set(string dateString, IUser user)
        {
            try
            {
                var soberDate = DateTime.Parse(dateString);
                _databaseService.SetDate(Context.Guild.Id, user.Id, user.Username, soberDate);
                return ReplyAsync($"Sober date set to {soberDate.ToShortDateString()} for {user.Username}");
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

        [Command("reset")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [Summary("Resets a given user's sobriety date to today.")]
        public Task Reset(IUser user)
        {
            var today = DateTime.Today;
            _databaseService.SetDate(Context.Guild.Id, user.Id, user.Username, today);
            return ReplyAsync($"Sober date reset to {today.ToShortDateString()} for {user.Username}");
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
        [Alias("delete")]
        [Summary("Take a break from sobriety and remove yourself from the database. :(")]
        public Task Break()
        {
            _databaseService.RemoveSobriety(Context.Guild.Id, Context.User.Id);
            return ReplyAsync($"{Context.User.Username} has been removed from the database.  Sorry to see you go :(");
        }

        [Command("break")]
        [Alias("delete")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [Summary("Remove a given user from the database.")]
        public Task Delete(IUser user)
        {
            _databaseService.RemoveSobriety(Context.Guild.Id, user.Id);
            return ReplyAsync($"{user.Username} has been removed from the database.");
        }

        [Command("milestones on")]
        [Summary("Enable milestone notifications.")]
        public Task MilestonesOn()
        {
            _databaseService.EnableMilestones(Context.Guild.Id, Context.User.Id);
            return ReplyAsync($"Milestones enabled for {Context.User.Username}.");
        }

        [Command("milestones off")]
        [Summary("Disable milestone notifications.")]
        public Task MilestonesOff()
        {
            _databaseService.DisableMilestones(Context.Guild.Id, Context.User.Id);
            return ReplyAsync($"Milestones disabled for {Context.User.Username}.");
        }

        [Command("config prunedays")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [Summary("Sets the number of days of inactivity before users are pruned from the database.")]
        public Task ConfigPrunedays(int days)
        {
            _databaseService.SetPruneDays(Context.Guild.Id, days);
            return ReplyAsync($"Users will now be removed from database after {days} days of inactivity.");
        }

        [Command("config milestonechannel")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [Summary("Sets the channel that milestone notifications are sent to.  By default, notifications will be sent to last channel user is active in.")]
        public Task ConfigMilestonechannel(IChannel channel)
        {
            _databaseService.SetMilestoneChannel(Context.Guild.Id, channel.Id);
            return ReplyAsync($"Milestones will be posted to <#{channel.Id}>");
        }

        [Command("config unset milestonechannel")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [Summary("Sets milestone notifications to be sent to last channel user is active in.")]
        public Task ConfigUnsetMilestonechannel()
        {
            _databaseService.SetMilestoneChannel(Context.Guild.Id, 0);
            return ReplyAsync($"Milestones will be posted to last channel user posted in.");
        }

        [Command("ban")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [Summary("Removes user from database and no longer allows them to user the bot.  Replies with given message instead.")]
        public Task Ban(IUser user, string message)
        {
            _databaseService.BanUser(Context.Guild.Id, user.Id, message);
            return ReplyAsync($"{user.Username} banned with message: {message}");
        }

        [Command("unban")]
        [RequireUserPermission(GuildPermission.Administrator, Group = "Permission")]
        [RequireOwner(Group = "Permission")]
        [Summary("Allow banned user to use the bot once again.")]
        public Task Unban(IUser user)
        {
            _databaseService.UnbanUser(Context.Guild.Id, user.Id);
            return ReplyAsync($"{user.Username} has been unbanned.");
        }
    }
}