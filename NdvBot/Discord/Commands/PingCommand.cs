using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace NdvBot.Discord.Commands
{
    public class PingCommand : BaseCommandModule
    {
        [Command("ping")]
        [Description("Pong!")]
        public Task Ping(CommandContext ctx) => ctx.RespondAsync("pong!");
        
    }
}