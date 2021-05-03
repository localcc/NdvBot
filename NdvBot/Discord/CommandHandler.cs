using System;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using NdvBot.Database.Mongo;
using NdvBot.Discord.Database;
using Optional = Discord.Optional;

namespace NdvBot.Discord
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;

        public CommandHandler(IServiceProvider services, ILogger logger, CommandService commands, DiscordSocketClient client)
        {
            this._services = services;
            this._logger = logger;
            this._commands = commands;
            this._client = client;
        }

        public async Task InitializeAsync()
        {
            this._client.MessageReceived += this.MessageReceived;
            this._commands.CommandExecuted += this.CommandExecuted;
            
            await this._commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: this._services);
        }

        private async Task MessageReceived(SocketMessage msg)
        {
            try
            {
                var message = msg as SocketUserMessage;
                if (message is null) return;
                if (message.Author.IsBot) return;

                string prefix = ">>";
                if (msg.Channel is not IGuildChannel guildChannel) return;
                var mongoConnection = this._services.GetService(typeof(IMongoConnection)) as IMongoConnection;
                if (mongoConnection is null)
                {
                    throw new DataException("MongoDB Unavailable");
                }

                var f1 = Builders<GuildData>.Filter.Eq("GuildId", guildChannel.Guild.Id);
                var guildDataCollection =
                    mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton);
                var guildData = (await guildDataCollection.FindAsync(f1)).FirstOrDefault();
                if (guildData is null)
                {
                    guildData = new GuildData(guildChannel.Guild.Id, ">>");
                    await guildDataCollection.InsertOneAsync(guildData);
                }

                prefix = guildData.Prefix;

                int argPos = 0;
                if (!(message.HasStringPrefix(prefix, ref argPos))) return;

                var context = new CustomCommandContext(guildData, this._client, message);
                await this._commands.ExecuteAsync(context: context, argPos: argPos, services: this._services);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private async Task CommandExecuted(global::Discord.Optional<CommandInfo> command,
            ICommandContext commandContext, IResult result)
        {
            if (!string.IsNullOrEmpty(result?.ErrorReason))
            {
                await commandContext.Channel.SendMessageAsync(result.ErrorReason);
            }
            
            var commandName = command.IsSpecified ? command.Value.Name : "A command";
            await this._logger.Log(new LogMessage(LogSeverity.Info, 
                "CommandExecution", 
                $"{commandName} was executed at {DateTime.UtcNow}."));
        }
    }
}