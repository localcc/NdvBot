using System.Threading.Tasks;
using Discord.WebSocket;

namespace NdvBot.Discord
{
    public interface IClient
    {
        public DiscordSocketClient DiscordClient {get;}
        public Task Start(string token);
    }
}