using System.Threading.Tasks;
using Discord.Commands;

namespace NdvBot.Discord.Commands
{
    public class PingCommand : ModuleBase<CustomCommandContext>
    {
        [Command("ping")]
        [Summary("Pong!")]
        public Task Ping() => ReplyAsync("Pong");
    }
}