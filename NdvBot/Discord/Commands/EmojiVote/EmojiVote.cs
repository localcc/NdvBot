using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MongoDB.Driver;
using NdvBot.Database.Mongo;
using NdvBot.Discord.Database;

namespace NdvBot.Discord.Commands.EmojiVote
{
    public class EmojiVote : BaseCommandModule
    {
        private readonly IMongoConnection _mongoConnection;
        
        public EmojiVote(IMongoConnection mongoConnection)
        {
            this._mongoConnection = mongoConnection;
        }

        
        public const string Tick = "✅";
        public const string Cross = "❌";
        
        [RequireUserPermissions(Permissions.Administrator)]
        [Command("sendEmojiRoleReaction")]
        [Description("Sends a message which upon reacting on gives or removes emoji role")]
        public async Task SendEmojiRoleReaction(CommandContext ctx)
        {
            var guildData = await ctx.GetGuildData(this._mongoConnection);
            if (guildData is null) return;
            if (guildData.EmojiVoteData is null)
            {
                guildData.EmojiVoteData = new();
            }


            var msg = await ctx.Channel.SendMessageAsync("React on this message to get/remove emoji role!");
            await msg.CreateReactionAsync(DiscordEmoji.FromUnicode(Tick));
            await msg.CreateReactionAsync(DiscordEmoji.FromUnicode(Cross));
            if(!guildData.EmojiVoteData.ReactionMessages.TryGetValue(msg.Channel.Id, out var channelMessages))
            {
                channelMessages = new();
                guildData.EmojiVoteData.ReactionMessages.Add(msg.Channel.Id, channelMessages);
            }
            channelMessages.Add(msg.Id);
            
            var filter = Builders<GuildData>.Filter.Eq("_id", guildData._id);
            var update = Builders<GuildData>.Update.Set("EmojiVoteData", guildData.EmojiVoteData);
            await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindOneAndUpdateAsync(filter, update);
        }
        
    }
}