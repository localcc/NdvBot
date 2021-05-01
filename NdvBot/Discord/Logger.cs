using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace NdvBot.Discord
{
    public class Logger : ILogger
    {
        public Logger(DiscordSocketClient discordClient, CommandService commands)
        {
            discordClient.Log += this.Log;
            commands.Log += this.Log;
        }

        public async Task Log(LogMessage msg)
        {
            if (msg.Exception is CommandException cmdException)
            {
                // We can tell the user that something unexpected has happened
                await cmdException.Context.Channel.SendMessageAsync("Something went catastrophically wrong!");

                // We can also log this incident
                Console.WriteLine($"{cmdException.Context.User} failed to execute '{cmdException.Command.Name}' in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException.ToString());
            }
            else
            {
                Console.WriteLine($"[Log/{msg.Severity}] {msg.Message}");
            }
        }
    }
}