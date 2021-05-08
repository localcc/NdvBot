using System.Threading.Tasks;
using DSharpPlus;

namespace NdvBot.Discord
{
    public interface IClient
    {
        public DiscordShardedClient DiscordClient {get;}
        public Task Start(string token);
    }
}