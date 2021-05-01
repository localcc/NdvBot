using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MongoDB.Driver;
using NdvBot.Database.Mongo;
using NdvBot.Discord.Database;

namespace NdvBot.Discord.Commands.Shitpost
{
    public class Shitpost : ModuleBase<CustomCommandContext>
    {
        private readonly IMongoConnection _mongoConnection;
        
        public Shitpost(IMongoConnection mongoConnection)
        {
            this._mongoConnection = mongoConnection;
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


            var chunkBuilder = new ChunkStringBuilder("glsl");
            var removeQueue = new List<ulong>();

            for (var i = 0; i < data.ChannelQueue.Count; i++)
            {
                StringBuilder localBuilder = new();
                var user = this.Context.Client.GetUser(data.ChannelQueue[i]);
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

            Task t1;
            if (data.ChannelQueue.Count != 0)
            {
                t1 = chunkBuilder.PrintOut((builder) => ReplyAsync(builder.ToString()));
            }
            else
            {
                t1 = ReplyAsync("Queue is empty!");
            }

            data.ChannelQueue.RemoveAll(e => removeQueue.Contains(e));

            var filter = Builders<GuildData>.Filter.Eq("_id", this.Context.Data._id);
            var update = Builders<GuildData>.Update.Set("ShitPostData", data);
            var t2 = this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindOneAndUpdateAsync(filter, update);
            await Task.WhenAll(t1, t2);
            return CommandResult.FromSuccess();
        }

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
        
    }
}