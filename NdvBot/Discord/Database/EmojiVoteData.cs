using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace NdvBot.Discord.Database
{
    public class EmojiVoteData
    {
        public ulong? ChannelId { get; set; }
        public ulong? EmojiRole { get; set; }
        public List<ulong> EmojiIds { get; set; } = new();
        public List<ulong> VotedMessages { get; set; } = new();
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ulong, List<ulong>> ReactionMessages { get; set; } = new();

        public EmojiVoteData()
        {
            
        }

        public async Task<DiscordRole> GetOrCreateEmojiRole(DiscordClient client, DiscordGuild guild)
        {
            var role = guild.GetRole(this.EmojiRole ?? 0);
            if (role is null)
            {
                role = await guild.CreateRoleAsync("emojis");
            }

            this.EmojiRole = role.Id;

            return role;
        }
    }
}