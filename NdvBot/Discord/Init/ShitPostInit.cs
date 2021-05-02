using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.WebSocket;
using MongoDB.Driver;
using NdvBot.Database.Mongo;
using NdvBot.Discord.Commands.Shitpost;
using NdvBot.Discord.Database;

namespace NdvBot.Discord.Init
{
    public class ShitPostInit : IInit
    {
        public long Priority { get; set; } = 10;

        
        private Timer _timer;
        private IMongoConnection _mongoConnection;
        private DiscordSocketClient _socketClient;

        public Task Init(IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetService(typeof(DiscordSocketClient)) as DiscordSocketClient;
            var mongoConnection = serviceProvider.GetService(typeof(IMongoConnection)) as IMongoConnection;
            this._socketClient = client ?? throw new DataException("Failed to get client from service provider");
            this._socketClient.ReactionAdded += this.ReactionAdded;
            this._mongoConnection =
                mongoConnection ?? throw new DataException("Failed to get database from service provider");
            
            var midnight = DateTime.Today.AddDays(1);
            var adjust = (midnight - DateTime.Now).TotalMilliseconds;
            
            this._timer = new Timer(adjust); // adjusting for running in sync with hour
            this._timer.Elapsed += this.RunShitPost;
            this._timer.Enabled = true;
            this._timer.AutoReset = true;
            return Task.CompletedTask;
        }

        private async Task ReactionAdded(Cacheable<IUserMessage, ulong> msg, ISocketMessageChannel channel,
            SocketReaction reaction)
        {
            var message = await msg.GetOrDownloadAsync();
            if (message is null) return;
            if (!reaction.User.IsSpecified) return;
            if (reaction.User.Value.IsBot) return;
            if (channel is not SocketGuildChannel socketChannel) return;
            var f = Builders<GuildData>.Filter.Eq("GuildId", socketChannel.Guild.Id);
            var data = await (await this._mongoConnection.ServerDb
                .GetCollection<GuildData>(MongoCollections.GuildDataColleciton).FindAsync(f)).FirstOrDefaultAsync();
            if (data is null) return;
            
            
            if (data.ShitPostData is null) return;
            if(!data.ShitPostData.ReactionMessages.TryGetValue(socketChannel.Id, out var channelMessages)) return;
            if (!channelMessages.Contains(msg.Id)) return;

            if (reaction.Emote.Name == Shitpost.Tick)
            {
                if (!data.ShitPostData.ChannelQueue.Contains(reaction.User.Value.Id))
                {
                    data.ShitPostData.ChannelQueue.Add(reaction.User.Value.Id);
                }
            }else if (reaction.Emote.Name == Shitpost.Cross)
            {
                data.ShitPostData.ChannelQueue.Remove(reaction.User.Value.Id);
            }

            var taskList = new List<Task>();
            taskList.Add(message.RemoveReactionAsync(reaction.Emote, reaction.User.Value));

            taskList.Add(Shitpost.UpdateMessagesContent(this._socketClient, socketChannel.Guild, data.ShitPostData).ContinueWith(
                (e) =>
                {
                    var filter = Builders<GuildData>.Filter.Eq("_id", data._id);
                    var update = Builders<GuildData>.Update.Set("ShitPostData", data.ShitPostData);
                    taskList.Add(this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                        .FindOneAndUpdateAsync(filter, update));
                }));
            await Task.WhenAll(taskList);
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
                var guild = this._socketClient.GetGuild(guildId);
                if (guild is null) continue;
                var channel = guild.GetTextChannel(channelId);
                if (channel is null)
                {
                    var filter = Builders<GuildData>.Filter.Eq("_id", guildData._id);
                    var update = Builders<GuildData>.Update.Set<ShitPostData?>("ShitPostData", null);
                    await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                        .FindOneAndUpdateAsync(filter, update);
                    continue;
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
                    continue;
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