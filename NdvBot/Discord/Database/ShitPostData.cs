using System;
using System.Collections.Generic;

namespace NdvBot.Discord.Database
{
    public class ShitPostData
    {
        public ulong ShitPostChannelId { get; set; }
        public List<ulong> ChannelQueue { get; set; } = new();
        public List<ulong> ReactionMessages { get; set; } = new();

        public ShitPostData()
        {
            
        }

        public ShitPostData(ulong shitPostChannelId)
        {
            this.ShitPostChannelId = shitPostChannelId;
        }
    }
}