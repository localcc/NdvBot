using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace NdvBot.Discord.Database
{
    public class ShitPostData
    {
        public ulong ShitPostChannelId { get; set; }
        public List<ulong> ChannelQueue { get; set; } = new();
        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
        public Dictionary<ulong, List<ulong>> ReactionMessages { get; set; } = new();

        public ShitPostData()
        {
            
        }

        public ShitPostData(ulong shitPostChannelId)
        {
            this.ShitPostChannelId = shitPostChannelId;
        }
    }
}