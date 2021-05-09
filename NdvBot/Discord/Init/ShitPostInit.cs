using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
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
        private DiscordShardedClient _socketClient;

        public Task Init(IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetService(typeof(DiscordShardedClient)) as DiscordShardedClient;
            var mongoConnection = serviceProvider.GetService(typeof(IMongoConnection)) as IMongoConnection;
            this._socketClient = client ?? throw new DataException("Failed to get client from service provider");
            this._socketClient.MessageReactionAdded += this.ReactionAdded;
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

        private async Task ReactionAdded(DiscordClient client, MessageReactionAddEventArgs args)
        {
            var msg = args.Message;
            if (msg is null) return;
            var reaction = args.Emoji;
            if (reaction is null) return;
            if (args.User.IsBot) return;
            var f = Builders<GuildData>.Filter.Eq("GuildId", args.Guild.Id);
            var data = await (await this._mongoConnection.ServerDb
                .GetCollection<GuildData>(MongoCollections.GuildDataColleciton).FindAsync(f)).FirstOrDefaultAsync();
            if (data is null) return;
            
            
            if (data.ShitPostData is null) return;
            if(!data.ShitPostData.ReactionMessages.TryGetValue(args.Channel.Id, out var channelMessages)) return;
            if (!channelMessages.Contains(msg.Id)) return;

            Console.WriteLine(reaction.Name);
            if (reaction.Name == Shitpost.Tick)
            {
                if (!data.ShitPostData.ChannelQueue.Contains(args.User.Id))
                {
                    data.ShitPostData.ChannelQueue.Add(args.User.Id);
                }
            }else if (reaction.Name == Shitpost.Cross)
            {
                data.ShitPostData.ChannelQueue.Remove(args.User.Id);
            }

            var taskList = new List<Task>();
            taskList.Add(args.Message.DeleteReactionAsync(reaction, args.User));

            taskList.Add(Shitpost.UpdateMessagesContent(client, args.Guild, data.ShitPostData).ContinueWith(
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
                var socketClient = this._socketClient.GetShard(guildId);
                
                try
                {
                var guild = await socketClient.GetGuildAsync(guildId);
                if (guild is null) continue;
                var channel = guild.GetChannel(channelId);
                if (channel is null || channel.Type != ChannelType.Text)
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
                    if(channelPermissionOverwrite.Type == OverwriteType.Member)
                    {
                        var overwriteUser = await channelPermissionOverwrite.GetMemberAsync();
                        if (overwriteUser is null) continue;
                        tasks.Add(channel.AddOverwriteAsync(overwriteUser, Permissions.None, Permissions.None));
                    }
                }
                await Task.WhenAll(tasks);
                tasks.Clear();

                if (guildData.ShitPostData!.ChannelQueue.Count == 0)
                {
                    await channel.ModifyAsync((props) => props.Name = $"{guild.Owner.Username}-shitpost");
                    continue;
                }

                    var userId = guildData.ShitPostData.ChannelQueue.First();
                    var user = await guild.GetMemberAsync(userId);
                    guildData.ShitPostData.ChannelQueue.Remove(userId);
                    while (user is null && guildData.ShitPostData.ChannelQueue.Count != 0)
                    {
                        userId = guildData.ShitPostData.ChannelQueue.First();
                        guildData.ShitPostData.ChannelQueue.Remove(userId);
                    }

                    if (user is not null)
                    {
                        var t1 = channel.AddOverwriteAsync(user, Permissions.SendMessages);
                        var t2 = channel.ModifyAsync((props) => props.Name = $"{user.Username}-shitpost");
                        await Task.WhenAll(t1, t2);
                    }

                    await Shitpost.UpdateMessagesContent(socketClient, guild, guildData.ShitPostData).ContinueWith(
                        async (e) =>
                        {

                            var filter = Builders<GuildData>.Filter.Eq("_id", guildData._id);
                            var update = Builders<GuildData>.Update.Set("ShitPostData", guildData.ShitPostData);
                            await this._mongoConnection.ServerDb
                                .GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                                .FindOneAndUpdateAsync(filter, update);
                        });
                }
                catch (Exception e)
                {
                    if (e is not InvalidOperationException && e is not UnauthorizedException)
                    {
                        throw;
                    }
                }
            }
        }

        public Task DeInit()
        {
            throw new System.NotImplementedException();
        }
    }
}