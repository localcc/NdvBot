using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MongoDB.Driver;
using NdvBot.Database.Mongo;
using NdvBot.Discord.Database;

namespace NdvBot.Discord.Commands.Settings
{
    public class Settings : ModuleBase<CustomCommandContext>
    {
        private readonly IMongoConnection _mongoConnection;
        
        public Settings(IMongoConnection mongoConnection)
        {
            this._mongoConnection = mongoConnection;
        } 
        
        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("setPrefix")]
        [Summary("Changes command prefix, default: `!`")]
        public async Task<RuntimeResult> SetPrefix(string? newPrefix)
        {
            if (newPrefix is null || newPrefix.Length > 3)
            {
                //todo: localization
                return CommandResult.FromError("Invalid prefix!");
            } 
            
            var f1 = Builders<GuildData>.Filter.Eq("GuildId", Context.Guild.Id);
            var update = Builders<GuildData>.Update.Set("Prefix", newPrefix);
            await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindOneAndUpdateAsync(f1, update);
            //todo: localization
            return CommandResult.FromSuccess("Prefix set!");
        }
    }
}