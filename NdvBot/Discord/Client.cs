using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NdvBot.Config;
using NdvBot.Discord.Init;

namespace NdvBot.Discord
{
    public class Client : IClient
    {
        public DiscordShardedClient DiscordClient {get;}
        private readonly IServiceProvider _serviceProvider;

        public Client(IServiceProvider serviceProvider, DiscordShardedClient discordClient)
        {
            this.DiscordClient = discordClient;
            this._serviceProvider = serviceProvider;
        }

        public async Task Start(string token)
        { 
            await new CommandHandler(this._serviceProvider, this.DiscordClient).InitializeAsync();
            await this.DiscordClient.UseInteractivityAsync(new InteractivityConfiguration
            {
                PollBehaviour = PollBehaviour.DeleteEmojis,
            });

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

            this.DiscordClient.GuildCreated += this.JoinedGuild;
        }

        private async Task JoinedGuild(DiscordClient client, GuildCreateEventArgs args)
        {
            var botUser = await args.Guild.GetMemberAsync(this.DiscordClient.CurrentUser.Id);
            await botUser.ModifyAsync((props) =>
            {
                props.Nickname = $"[>>] {this.DiscordClient.CurrentUser.Username}";
            });
        }
    }
}