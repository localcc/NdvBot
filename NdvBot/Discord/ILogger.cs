using System.Threading.Tasks;
using Discord;

namespace NdvBot.Discord
{
    public interface ILogger
    {
        public Task Log(LogMessage msg);
    }
}