using System;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NdvBot.Database.Mongo;
using NdvBot.Discord.Database;

namespace NdvBot.Discord
{
    public class CommandHandler
    {
        private readonly DiscordShardedClient _client;
        private readonly IServiceProvider _services;

        public CommandHandler(IServiceProvider services, DiscordShardedClient client)
        {
            this._services = services;
            this._client = client;
        }

        private async Task<int> PrefixResolver(DiscordMessage msg)
        {
            string prefix = ">>";
            var guild = msg.Channel.Guild;
            if (guild is null)
            {
                if (!msg.Content.StartsWith(">>")) return -1;
            }
            else
            {

                var mongoConnection = this._services.GetService(typeof(IMongoConnection)) as IMongoConnection;
                if (mongoConnection is null)
                {
                    throw new DataException("MongoDB Unavailable");
                }

                var f1 = Builders<GuildData>.Filter.Eq("GuildId", guild.Id);
                var guildDataCollection =
                    mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton);
                var guildData = (await guildDataCollection.FindAsync(f1)).FirstOrDefault();
                if (guildData is null)
                {
                    guildData = new GuildData(guild.Id, ">>");
                    await guildDataCollection.InsertOneAsync(guildData);
                }

                prefix = guildData.Prefix;
                if (!msg.Content.StartsWith(prefix)) return -1;
            }

            return prefix.Length;
        }

        public async Task InitializeAsync()
        {
            var commandsByShard = await this._client.UseCommandsNextAsync(new CommandsNextConfiguration
            {
                Services = this._services,
                PrefixResolver = this.PrefixResolver,
            });
            foreach (var commands in commandsByShard)
            {
                commands.Value.SetHelpFormatter<HelpFormatter>();
                commands.Value.RegisterCommands(Assembly.GetExecutingAssembly());   
            }
        }
    }
}