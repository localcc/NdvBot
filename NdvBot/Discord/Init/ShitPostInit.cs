using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using MongoDB.Driver;
using NdvBot.Database.Mongo;
using NdvBot.Discord.Database;

namespace NdvBot.Discord.Init
{
    public class ShitPostInit : IInit
    {
        public long Priority { get; set; } = 10;

        
        private Timer _timer;
        private IClient _client;
        private IMongoConnection _mongoConnection;

        public Task Init(IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetService(typeof(IClient)) as IClient;
            var mongoConnection = serviceProvider.GetService(typeof(IMongoConnection)) as IMongoConnection;
            this._client = client ?? throw new DataException("Failed to get client from service provider");
            this._mongoConnection =
                mongoConnection ?? throw new DataException("Failed to get database from service provider");
            
            var midnight = DateTime.Now.AddSeconds(10);
            var adjust = (midnight - DateTime.Now).TotalMilliseconds;
            
            this._timer = new Timer(adjust); // adjusting for running in sync with hour
            this._timer.Elapsed += this.RunShitPost;
            this._timer.Enabled = true;
            this._timer.AutoReset = true;
            return Task.CompletedTask;
        }

        private async void RunShitPost(object sender, ElapsedEventArgs args)
        {
            this._timer.Interval = new TimeSpan(1, 0, 0, 0).TotalMilliseconds;

            var guildFilter = Builders<GuildData>.Filter.Ne<ShitPostData?>("ShitPostData", null);
            var guilds = await (await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton).FindAsync(guildFilter)).ToListAsync();
            if (guilds.Count == 0) return;
            foreach (var guildData in guilds)
            {
                var guildId = guildData.GuildId;
                var channelId = guildData.ShitPostData!.ShitPostChannelId;
                var guild = this._client.DiscordClient.GetGuild(guildId);
                if (guild is null) return;
                var channel = guild.GetTextChannel(channelId);
                if (channel is null)
                {
                    var filter = Builders<GuildData>.Filter.Eq("_id", guildData._id);
                    var update = Builders<GuildData>.Update.Set<ShitPostData?>("ShitPostData", null);
                    await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                        .FindOneAndUpdateAsync(filter, update);
                    return;
                }
                
                var tasks = new List<Task>(channel.PermissionOverwrites.Count);
                foreach (var channelPermissionOverwrite in channel.PermissionOverwrites)
                {
                    if(channelPermissionOverwrite.TargetType == PermissionTarget.User)
                    {
                        var overwriteUser = guild.GetUser(channelPermissionOverwrite.TargetId);
                        if (overwriteUser is null) continue;
                        tasks.Add(channel.RemovePermissionOverwriteAsync(overwriteUser));
                    }
                }

                if (guildData.ShitPostData!.ChannelQueue.Count == 0)
                {
                    await channel.ModifyAsync((props) => props.Name = $"{guild.Owner.Username}-shitpost");
                    return;
                }

                try
                {
                    var userId = guildData.ShitPostData.ChannelQueue.First();
                    var user = guild.GetUser(userId);
                    guildData.ShitPostData.ChannelQueue.Remove(userId);
                    while (user is null && guildData.ShitPostData.ChannelQueue.Count != 0)
                    {
                        userId = guildData.ShitPostData.ChannelQueue.First();
                        guildData.ShitPostData.ChannelQueue.Remove(userId);
                    }

                    if (user is not null)
                    {
                        await channel.AddPermissionOverwriteAsync(user,
                            new OverwritePermissions(sendMessages: PermValue.Allow));
                        await channel.ModifyAsync((props) => props.Name = $"{user.Username}-shitpost");
                    }

                    var filter = Builders<GuildData>.Filter.Eq("_id", guildData._id);
                    var update = Builders<GuildData>.Update.Set("ShitPostData", guildData.ShitPostData);
                    await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                        .FindOneAndUpdateAsync(filter, update);
                }
                catch (InvalidOperationException)
                {
                    
                }
            }
        }

        public Task DeInit()
        {
            throw new System.NotImplementedException();
        }
    }
}