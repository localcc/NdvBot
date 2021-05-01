using Discord.Commands;
using Discord.WebSocket;
using NdvBot.Discord.Database;

namespace NdvBot.Discord
{
    public class CustomCommandContext : SocketCommandContext
    {
        public GuildData Data { get; }
        
        public CustomCommandContext(GuildData data, DiscordSocketClient client, SocketUserMessage msg) : base(client, msg)
        {
            this.Data = data;
        }
    }
}