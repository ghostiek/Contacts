﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using Discord.Commands;
using Discord.WebSocket;
using ContactsBot.Configuration;

namespace ContactsBot
{
    public class CommandHandler : IMessageAction
    {
        private Program _programContext;
        private CommandService _commands;
        private DiscordSocketClient _client;
        private BotConfiguration _config;
        IDependencyMap _map;
        public bool IsEnabled { get; private set; }

        public async void Install(IDependencyMap map)
        {
            _map = map;
            _client = _map.Get<DiscordSocketClient>();
            _config = await _map.Get<ConfigManager>().GetConfig<BotConfiguration>();
            _programContext = _map.Get<Program>();
            _commands = new CommandService();
            _map.Add(_commands);
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
        }

        public void Enable()
        {
            _client.MessageReceived += HandleCommandAsync;
            IsEnabled = true;
        }

        public void Disable()
        {
            _client.MessageReceived -= HandleCommandAsync;
            IsEnabled = false;
        }

        private async Task HandleCommandAsync(SocketMessage msg)
        {
            var message = msg as SocketUserMessage;
            if (message == null) return;

            int argPos = 0;
#if DEV
            if (msg.Channel.Name == (_config.FilterChannel ?? msg.Channel.Name))
#endif
                if (message.HasCharPrefix(_config.PrefixCharacter, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))
                {
                    var context = new CommandContext(_client, message);

                    // Provide the dependency map when executing commands
                    var result = await _commands.ExecuteAsync(context, argPos, _map);
                    if (!result.IsSuccess)
                    {
                        if ((result is SearchResult))
                            return;
                        if (result is ExecuteResult)
                            await message.Channel.SendMessageAsync("```" + ((ExecuteResult)result).Exception.ToString() + "```");
                        else
                            await message.Channel.SendMessageAsync(result.ErrorReason);
                    }
                    else
                        await _programContext.ChannelLog_CommandLogAsync($"\"{message.Author.Username}\" ran the following command: {message.Content}");
                }
        }
    }
}
