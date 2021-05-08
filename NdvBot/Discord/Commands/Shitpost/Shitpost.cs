using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MongoDB.Driver;
using MongoDB.Driver.Core.WireProtocol.Messages;
using NdvBot.Database.Mongo;
using NdvBot.Discord.Database;

namespace NdvBot.Discord.Commands.Shitpost
{
    public class Shitpost : BaseCommandModule
    {
        private readonly IMongoConnection _mongoConnection;
        private readonly DiscordShardedClient _socketClient;
        
        
        public const string Tick = "✅";
        public const string Cross = "❌";
        
        public Shitpost(IMongoConnection mongoConnection, DiscordShardedClient socketClient)
        {
            this._mongoConnection = mongoConnection;
            this._socketClient = socketClient;
        }

        public static async Task<(ChunkStringBuilder builder, List<ulong> removeQueue)> GetQueueMsgContent(DiscordClient client, ShitPostData data)
        { 
            var chunkBuilder = new ChunkStringBuilder("glsl");
            var removeQueue = new List<ulong>();

            if (data.ChannelQueue.Count == 0)
            {
                //todo: localization
                chunkBuilder.Append("The queue is empty for now");
                return (chunkBuilder, removeQueue);
            }
            for (var i = 0; i < data.ChannelQueue.Count; i++)
            {
                StringBuilder localBuilder = new();
                var user = 
                    await client.GetUserAsync(data.ChannelQueue[i]);
                if (user is null)
                {
                    removeQueue.Add(data.ChannelQueue[i]);
                    continue;
                }

                localBuilder.Append(i);
                localBuilder.Append(": ");
                localBuilder.Append(user.Username);
                localBuilder.Append("#");
                localBuilder.Append(user.Discriminator);
                if (i != data.ChannelQueue.Count - 1)
                {
                    localBuilder.Append("\n");
                }

                chunkBuilder.Append(localBuilder);
            }

            return (chunkBuilder, removeQueue);
        }
        

        [RequireUserPermissions(Permissions.Administrator)]
        [RequireBotPermissions(Permissions.ManageChannels)]
        [Command("setShitpostChannel")]
        [DSharpPlus.CommandsNext.Attributes.Description("Adds you or other user to shitpost queue")]
        public async Task SetShitpostChannel(CommandContext ctx, DiscordChannel channel)
        {
            var filter = Builders<GuildData>.Filter.Eq("GuildId", channel.Guild.Id);
            var newData = new ShitPostData(channel.Id);
            var update = Builders<GuildData>.Update.Set("ShitPostData", newData);
            await this._mongoConnection.ServerDb
                .GetCollection<GuildData>(MongoCollections.GuildDataColleciton).FindOneAndUpdateAsync(filter, update);
            await ctx.RespondAsync($"Set {channel.Mention} as shitpost channel successfully!");
        }
        
        [RequireBotPermissions(Permissions.SendMessages)]
        [Command("getShitpostQueue")]
        [DSharpPlus.CommandsNext.Attributes.Description("Shows shitpost channel queue")]
        public async Task GetShitPostQueue(CommandContext ctx)
        {
            var guildData = await ctx.GetGuildData(this._mongoConnection);
            var socketClient = this._socketClient.GetShard(ctx.Guild);
            if (guildData is null) return;
            var data = guildData.ShitPostData;
            if (data is null)
            {
                //todo: localization
                await ctx.RespondAsync("Shit post channel is not set!");
                return;
            }
            //todo: localization

            var chunkBuilder = await Shitpost.GetQueueMsgContent(socketClient, data);

            Task t1;
            if (data.ChannelQueue.Count != 0)
            {
                t1 = chunkBuilder.builder.PrintOut((builder) => ctx.RespondAsync(builder.ToString()));
            }
            else
            {
                t1 = ctx.RespondAsync("Queue is empty!");
            }

            data.ChannelQueue.RemoveAll(e => chunkBuilder.removeQueue.Contains(e));

            var filter = Builders<GuildData>.Filter.Eq("_id", guildData._id);
            var update = Builders<GuildData>.Update.Set("ShitPostData", data);
            var t2 = this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindOneAndUpdateAsync(filter, update);
            await Task.WhenAll(t1, t2);
        }
        
        [RequireBotPermissions(Permissions.SendMessages)]
        [Command("addToShitpost")]
        [DSharpPlus.CommandsNext.Attributes.Description("Adds you or other user to shitpost queue")]
        public async Task AddToShitpost(CommandContext ctx, DiscordUser? user = null)
        {
            var guildData = await ctx.GetGuildData(this._mongoConnection);
            var socketClient = this._socketClient.GetShard(ctx.Guild);
            if (guildData is null) return;
            if (guildData.ShitPostData is null)
            {
                //todo: localization
                await ctx.RespondAsync("Shit post channel is not set");
                return;
            }

            if (user is not null)
            {
                var guildUser = await ctx.Guild.GetMemberAsync(ctx.User.Id);
                if ((guildUser.PermissionsIn(ctx.Channel) & Permissions.Administrator) != Permissions.Administrator)
                {
                    //todo: localization
                    await ctx.RespondAsync("Not enoguh permissions!");
                    return;
                }
            }
            else
            {
                user = ctx.User;
            }
            if (guildData.ShitPostData.ChannelQueue.Contains(user.Id))
            {
                //todo: localization
                await ctx.RespondAsync("You can't add a user twice to the queue!");
                return;
            }

            guildData.ShitPostData.ChannelQueue.Add(user.Id);

            await Shitpost.UpdateMessagesContent(socketClient, ctx.Guild, guildData.ShitPostData)
                .ContinueWith(
                    async (e) =>
                    {

                        var filter = Builders<GuildData>.Filter.Eq("_id", guildData._id);
                        var update = Builders<GuildData>.Update.Set("ShitPostData", guildData.ShitPostData);
                        await this._mongoConnection.ServerDb
                            .GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                            .FindOneAndUpdateAsync(filter, update);
                    });

            await ctx.RespondAsync($"Added {user.Mention} to the queue!");
        }
        
        [RequireBotPermissions(Permissions.SendMessages)]
        [Command("removeFromShitpost")]
        [DSharpPlus.CommandsNext.Attributes.Description("Removes you or other user from shitpost queue")]
        public async Task RemoveFromShitpost(CommandContext ctx, DiscordUser? user = null)
        {
            var guildData = await ctx.GetGuildData(this._mongoConnection);
            var socketClient = this._socketClient.GetShard(ctx.Guild);
            if (guildData is null) return;
            if (guildData.ShitPostData is null)
            {
                //todo: localization
                await ctx.RespondAsync("Shit post channel is not set!");
                return;
            }
            if (user is not null)
            {
                var guildUser = await ctx.Guild.GetMemberAsync(ctx.User.Id);
                if ((guildUser.PermissionsIn(ctx.Channel) & Permissions.Administrator) != Permissions.Administrator)
                {
                    //todo: localization
                    await ctx.RespondAsync("Not enough permissions!");
                    return;
                }
            }
            else
            {
                user = ctx.User;
            }

            if (!guildData.ShitPostData.ChannelQueue.Contains(user.Id))
            {
                await ctx.RespondAsync("The user is not in the queue!");
                return;
            }

            guildData.ShitPostData.ChannelQueue.Remove(user.Id);

            await Shitpost.UpdateMessagesContent(socketClient, ctx.Guild, guildData.ShitPostData)
                .ContinueWith(
                    async (e) =>
                    {
                        var filter = Builders<GuildData>.Filter.Eq("_id", guildData._id);
                        var update = Builders<GuildData>.Update.Set("ShitPostData", guildData.ShitPostData);
                        await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                            .FindOneAndUpdateAsync(filter, update);

                    });

            await ctx.RespondAsync($"Removed {user.Mention} from queue successfully!");
        }

        public static async Task UpdateMessagesContent(DiscordClient client, DiscordGuild guild, ShitPostData data)
        {
            var tasks = new List<Task>();
            
            var res = await Shitpost.GetQueueMsgContent(client, data);
            data.ChannelQueue.RemoveAll(e => res.removeQueue.Contains(e));
            
            var content = res.builder.AsOne(2000).ToString();
            
            foreach (var (channelId, messages) in data.ReactionMessages)
            {
                async Task<bool> UpdateMessage(DiscordChannel channel, ulong msgId, string content)
                {
                    var msg = await channel.GetMessageAsync(msgId);
                    if (msg is null) return false;
                    await msg.ModifyAsync((props) =>
                    {
                        props.Content = content;
                    });
                    return true;
                }
                
                async Task UpdateChannelMessages(DiscordGuild guild, List<ulong> msgs, string content)
                {
                    var channel = guild.GetChannel(channelId);
                    if (channel is null) return;
                    var tasks = new List<Task>();
                    
                    foreach (var msg in msgs)
                    {
                        tasks.Add(UpdateMessage(channel, msg, content).ContinueWith((res) =>
                        {
                            if (!res.Result)
                            {
                                msgs.Remove(msg);
                            }
                        }));
                    }

                    await Task.WhenAll(tasks);
                } 
                
                
                tasks.Add(UpdateChannelMessages(guild, messages, content));
            }

            await Task.WhenAll(tasks);
        }
        
        [RequireUserPermissions(Permissions.Administrator)]
        [RequireBotPermissions(Permissions.SendMessages)]
        [Command("sendShitpostReaction")]
        [DSharpPlus.CommandsNext.Attributes.Description("Sends shitpost reaction message that auto-updates")]
        public async Task SendReactionMessage(CommandContext ctx)
        {
            var guildData = await ctx.GetGuildData(this._mongoConnection);
            if (guildData is null) return;
            if (guildData.ShitPostData is null)
            {
                //todo: localization
                await ctx.RespondAsync("Guild has no shitpost channel set!");
                return;
            }

            ChunkStringBuilder builder = new("glsl");
            var shitPostData = guildData.ShitPostData;
            if (shitPostData.ChannelQueue.Count > 0)
            {
                for (var i = 0; i < shitPostData.ChannelQueue.Count; i++)
                {
                    StringBuilder localBuilder = new();
                    var user = await ctx.Client.GetUserAsync(shitPostData.ChannelQueue[i]);
                    localBuilder.Append(i);
                    localBuilder.Append(": ");
                    localBuilder.Append(user.Username);
                    localBuilder.Append("#");
                    localBuilder.Append(user.Discriminator);
                    if (i != shitPostData.ChannelQueue.Count - 1)
                    {
                        localBuilder.Append("\n");
                    }

                    builder.Append(localBuilder);
                }
            }
            else
            {
                builder.Append(new StringBuilder().Append("The queue is empty for now"));
            }

            await builder.PrintOut(async (b) =>
            {
                var msg = await ctx.Channel.SendMessageAsync(b.ToString());
                if (msg is null) return;
                await msg.CreateReactionAsync(DiscordEmoji.FromUnicode(Tick));
                await msg.CreateReactionAsync(DiscordEmoji.FromUnicode(Cross));
                if (!shitPostData.ReactionMessages.TryGetValue(msg.Channel.Id, out var channelMessages))
                {
                    channelMessages = new List<ulong>();
                    shitPostData.ReactionMessages.Add(msg.Channel.Id, channelMessages);
                }

                channelMessages.Add(msg.Id);
            });

            var filter = Builders<GuildData>.Filter.Eq("_id", guildData._id);
            var update = Builders<GuildData>.Update.Set("ShitPostData", shitPostData);
            await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindOneAndUpdateAsync(filter, update);
        }
        
    }
}