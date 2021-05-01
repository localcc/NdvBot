using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using NdvBot.Database.Mongo;
using NdvBot.Discord.Database;

namespace NdvBot.Discord.Commands.Shitpost
{
    public class Shitpost : ModuleBase<CustomCommandContext>
    {
        private readonly IMongoConnection _mongoConnection;
        private readonly DiscordSocketClient _socketClient;
        
        
        public const string Tick = "✅";
        public const string Cross = "❌";
        
        public Shitpost(IMongoConnection mongoConnection, DiscordSocketClient socketClient)
        {
            this._mongoConnection = mongoConnection;
            this._socketClient = socketClient;
        }

        public static (ChunkStringBuilder builder, List<ulong> removeQueue) GetQueueMsgContent(DiscordSocketClient client, ShitPostData data)
        { 
            var chunkBuilder = new ChunkStringBuilder("glsl");
            var removeQueue = new List<ulong>();

            for (var i = 0; i < data.ChannelQueue.Count; i++)
            {
                StringBuilder localBuilder = new();
                var user = client.GetUser(data.ChannelQueue[i]);
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
        

        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("setShitpostChannel")]
        [Summary("Sets shitpost channel")]
        public async Task<RuntimeResult> SetShitpostChannel(ITextChannel channel)
        {
            var filter = Builders<GuildData>.Filter.Eq("GuildId", this.Context.Guild.Id);
            var newData = new ShitPostData(channel.Id);
            var update = Builders<GuildData>.Update.Set("ShitPostData", newData);
            await this._mongoConnection.ServerDb
                .GetCollection<GuildData>(MongoCollections.GuildDataColleciton).FindOneAndUpdateAsync(filter, update);
            return CommandResult.FromSuccess($"Set {channel.Mention} as shitpost channel successfully!");
        }
        
        [Command("getShitpostQueue")]
        [Summary("Shows shitpost channel queue")]
        public async Task<RuntimeResult> GetShitPostQueue()
        {
            var data = this.Context.Data.ShitPostData;
            if (data is null)
            {
                //todo: localization
                return CommandResult.FromError("Shit post channel is not set");
            }
            //todo: localization

            var chunkBuilder = Shitpost.GetQueueMsgContent(this._socketClient, data);

            Task t1;
            if (data.ChannelQueue.Count != 0)
            {
                t1 = chunkBuilder.builder.PrintOut((builder) => ReplyAsync(builder.ToString()));
            }
            else
            {
                t1 = ReplyAsync("Queue is empty!");
            }

            data.ChannelQueue.RemoveAll(e => chunkBuilder.removeQueue.Contains(e));

            var filter = Builders<GuildData>.Filter.Eq("_id", this.Context.Data._id);
            var update = Builders<GuildData>.Update.Set("ShitPostData", data);
            var t2 = this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindOneAndUpdateAsync(filter, update);
            await Task.WhenAll(t1, t2);
            return CommandResult.FromSuccess();
        }
        //todo: add message update
        /*
        [Command("addToShitpost")]
        [Summary("Adds you or other user to shitpost queue")]
        public async Task<RuntimeResult> AddToShitpost(IUser? user = null)
        {
            if (this.Context.Data.ShitPostData is null)
            {
                //todo: localization
                return CommandResult.FromSuccess("Shit post channel is not set");
            }

            if (user is not null)
            {
                var guildUser = this.Context.Guild.GetUser(this.Context.User.Id);
                if (!guildUser.GuildPermissions.Administrator)
                {
                    //todo: localization
                    return CommandResult.FromError("Not enough permissions!");
                }
            }
            else
            {
                user = Context.User;
            }
            if (this.Context.Data.ShitPostData.ChannelQueue.Contains(user.Id))
            {
                //todo: localization
                return CommandResult.FromError("You can't add a user twice to the queue!");
            }

            this.Context.Data.ShitPostData.ChannelQueue.Add(user.Id);
            var filter = Builders<GuildData>.Filter.Eq("_id", this.Context.Data._id);
            var update = Builders<GuildData>.Update.Set("ShitPostData", this.Context.Data.ShitPostData);
            await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindOneAndUpdateAsync(filter, update);

            return CommandResult.FromSuccess($"Added {user.Mention} to the queue!");
        }
        
        [Command("removeFromShitpost")]
        [Summary("Removes you or other user from shitpost queue")]
        public async Task<RuntimeResult> RemoveFromShitpost(IUser? user = null)
        {
            if (this.Context.Data.ShitPostData is null)
            {
                //todo: localization
                return CommandResult.FromError("Shit post channel is not set!");
            }
            if (user is not null)
            {
                var guildUser = this.Context.Guild.GetUser(this.Context.User.Id);
                if (!guildUser.GuildPermissions.Administrator)
                {
                    //todo: localization
                    return CommandResult.FromError("Not enough permissions!");
                }
            }
            else
            {
                user = Context.User;
            }

            if (!this.Context.Data.ShitPostData.ChannelQueue.Contains(user.Id))
            {
                return CommandResult.FromError("The user is not in the queue!");
            }

            this.Context.Data.ShitPostData.ChannelQueue.Remove(user.Id);

            var filter = Builders<GuildData>.Filter.Eq("_id", this.Context.Data._id);
            var update = Builders<GuildData>.Update.Set("ShitPostData", this.Context.Data.ShitPostData);
            await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindOneAndUpdateAsync(filter, update);

            return CommandResult.FromSuccess($"Removed {user.Mention} from queue successfully!");
        }
*/
        
        [RequireUserPermission(GuildPermission.Administrator)]
        [Command("sendShitpostReaction")]
        [Summary("Sends shitpost reaction message that auto-updates")]
        public async Task<RuntimeResult> SendReactionMessage()
        {
            if (Context.Data.ShitPostData is null)
            {
                //todo: localization
                return CommandResult.FromError("Guild has no shitpost channel set!");
            }

            ChunkStringBuilder builder = new("glsl");
            var shitPostData = Context.Data.ShitPostData;
            if (shitPostData.ChannelQueue.Count > 0)
            {
                for (var i = 0; i < shitPostData.ChannelQueue.Count; i++)
                {
                    StringBuilder localBuilder = new();
                    var user = this.Context.Client.GetUser(shitPostData.ChannelQueue[i]);
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
                var msg = await ReplyAsync(b.ToString());
                if (msg is null) return;
                await msg.AddReactionAsync(new Emoji(Tick));
                await msg.AddReactionAsync(new Emoji(Cross));

                shitPostData.ReactionMessages.Add(msg.Id);
            });

            var filter = Builders<GuildData>.Filter.Eq("_id", this.Context.Data._id);
            var update = Builders<GuildData>.Update.Set("ShitPostData", shitPostData);
            await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindOneAndUpdateAsync(filter, update);
            return CommandResult.FromSuccess();
        }
    }
}