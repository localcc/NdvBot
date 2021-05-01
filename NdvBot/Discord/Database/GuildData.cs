using MongoDB.Bson;

namespace NdvBot.Discord.Database
{
    public class GuildData
    {
        public ObjectId _id { get; set; }
        public ulong GuildId { get; set; }
        public string Prefix { get; set; }
        public ShitPostData? ShitPostData { get; set; }

        public GuildData()
        {
            
        }

        public GuildData(ulong guildId, string prefix)
        {
            this.GuildId = guildId;
            this.Prefix = prefix;
        }
    }
}