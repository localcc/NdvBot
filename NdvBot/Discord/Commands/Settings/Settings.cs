using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using MongoDB.Driver;
using NdvBot.Database.Mongo;
using NdvBot.Discord.Database;

namespace NdvBot.Discord.Commands.Settings
{
    public class Settings : BaseCommandModule
    {
        private readonly IMongoConnection _mongoConnection;
        
        public Settings(IMongoConnection mongoConnection)
        {
            this._mongoConnection = mongoConnection;
        } 
        
        [RequireUserPermissions(Permissions.Administrator)]
        [RequireBotPermissions(Permissions.ManageNicknames)]
        [Command("setPrefix")]
        [Description("Changes command prefix, default: `>>`")]
        public async Task SetPrefix(CommandContext ctx, string? newPrefix)
        {            
            if (newPrefix is null || newPrefix.Length > 3)
            {
                //todo: localization
                await ctx.RespondAsync("Invalid prefix!");
                return;
            } 

            var botUser = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
            await botUser.ModifyAsync((props) =>
            {
                //space, two brackets
                if (botUser.Nickname.Length + newPrefix.Length + 3 >= 30) return;
                props.Nickname = $"[{newPrefix}] {botUser.Username}";
            });
            

            var f1 = Builders<GuildData>.Filter.Eq("GuildId", ctx.Guild.Id);
            var update = Builders<GuildData>.Update.Set("Prefix", newPrefix);
            await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindOneAndUpdateAsync(f1, update);
            //todo: localization
            await ctx.RespondAsync("Prefix set!");
        }
    }
}