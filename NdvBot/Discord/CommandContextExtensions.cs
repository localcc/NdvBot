using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using MongoDB.Driver;
using NdvBot.Database.Mongo;
using NdvBot.Discord.Database;

namespace NdvBot.Discord
{
    public static class CommandContextExtensions
    {
        public static async Task<GuildData?> GetGuildData(this CommandContext ctx, IMongoConnection mongoConnection)
        {
            if (ctx.Guild is null) return null;

            return await ctx.Guild.GetGuildData(mongoConnection);
        }

        public static async Task<GuildData?> GetGuildData(this DiscordGuild guild, IMongoConnection mongoConnection)
        {
            var filter = Builders<GuildData>.Filter.Eq("GuildId", guild.Id);
            return (await mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindAsync(filter)).FirstOrDefault();
        }
    }
}