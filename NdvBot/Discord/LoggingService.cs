using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NdvBot.Config;

namespace NdvBot.Discord
{
    public class LoggingService
    {
        public LoggingService(DiscordSocketClient client, CommandService command)
        {
            client.Log += this.LogAsync;
            command.Log += this.LogAsync;
        }

        private Task LogAsync(LogMessage msg)
        {
            if (msg.Exception is CommandException cmdException)
            {
                Console.WriteLine(
                    $"[Command/{msg.Severity}] {cmdException.Command.Aliases.First()} failed to execute in {cmdException.Context.Channel}.");
                Console.WriteLine(cmdException);
            }
            else
            {
                Console.WriteLine($"[General/{msg.Severity}] {msg}");
            }

            return Task.CompletedTask;
        }
    }
}