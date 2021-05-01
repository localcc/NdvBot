using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NdvBot.Config;
using NdvBot.Discord.Init;

namespace NdvBot.Discord
{
    public class Client : IClient
    {
        public DiscordSocketClient DiscordClient {get;}
        private readonly CommandService _commands;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;

        public Client(IServiceProvider serviceProvider, ILogger logger, DiscordSocketClient discordClient, CommandService commands)
        {
            this.DiscordClient = discordClient;
            this._logger = logger;
            this._commands = commands;
            this._serviceProvider = serviceProvider;
        }

        public async Task Start(string token)
        { 
            await new CommandHandler(this._serviceProvider, this._logger, this._commands, this.DiscordClient).InitializeAsync();
            await this.DiscordClient.LoginAsync(TokenType.Bot, token);
            await this.DiscordClient.StartAsync();


            foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                .Where(e => e.GetInterfaces().Contains(typeof(IInit)))
                .OrderByDescending(t => (((IInit) Activator.CreateInstance(t))!).Priority))
            {
                var init = Activator.CreateInstance(type) as IInit;
                if (init is null)
                {
                    throw new ApplicationException("Invalid IInit inheritance");
                }

                await init.Init(this._serviceProvider);
            }

            this.DiscordClient.JoinedGuild += this.JoinedGuild;
        }

        private async Task JoinedGuild(SocketGuild guild)
        {
            var botUser = guild.GetUser(this.DiscordClient.CurrentUser.Id);
            await botUser.ModifyAsync((props) =>
            {
                props.Nickname = $"[>>] {this.DiscordClient.CurrentUser.Username}";
            });
        }

    }
}