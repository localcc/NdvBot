using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
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

            var filter = Builders<GuildData>.Filter.Eq("GuildId", ctx.Guild.Id);
            return (await mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindAsync(filter)).FirstOrDefault();
        }
    }
}