using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordBot.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly DatabaseService _databaseService;
        private IServiceProvider _provider;

        public CommandHandlingService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, DatabaseService databaseService)
        {
            _discord = discord;
            _commands = commands;
            _provider = provider;
            _databaseService = databaseService;

            _discord.MessageReceived += MessageReceived;
        }

        public async Task InitializeAsync(IServiceProvider provider)
        {
            _provider = provider;
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), provider);
            // Add additional initialization code here...
        }

        private async Task MessageReceived(SocketMessage rawMessage)
        {
            // Ignore system messages and messages from bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            var context = new SocketCommandContext(_discord, message);
            _databaseService.UpdateActiveDate(context.Guild.Id, message.Author.Id);

            var milestoneName = _databaseService.GetNewMilestoneName(context.Guild.Id, message.Author.Id);

            if (milestoneName != null)
            {
                var milestoneChannel = _databaseService.GetMilestoneChannel(context.Guild.Id);
                var milestoneMessage = $"<@{message.Author.Id}> has reached a new milestone: {milestoneName}";
                if ((milestoneChannel ?? 0) > 0)
                {
                    await context.Guild
                        .GetTextChannel(milestoneChannel.Value)
                        .SendMessageAsync(milestoneMessage);
                }
                else
                {
                    await context.Channel.SendMessageAsync(milestoneMessage);
                }
            }

            int argPos = 0;
            if (!message.HasMentionPrefix(_discord.CurrentUser, ref argPos)) return;

            var banMessage = _databaseService.GetBanMessage(context.Guild.Id, message.Author.Id);
            if (banMessage != null)
            {
                await context.Channel.SendMessageAsync($"<@{message.Author.Id}> {banMessage}");
                return;
            }
            _databaseService.PruneInactiveUsers(context.Guild.Id);

            while (message.Content[argPos] == ' ') argPos++;

            var result = await _commands.ExecuteAsync(context, argPos, _provider);

            if (result.Error.Value == CommandError.UnknownCommand)
            {
                await context.Channel.SendMessageAsync($"Unknown command: {message.Content.Substring(message.Content.IndexOf('>') + 2)}");
            }
            else if (result.Error.HasValue)
            {
                await context.Channel.SendMessageAsync(result.ToString());
            }
        }
    }
}