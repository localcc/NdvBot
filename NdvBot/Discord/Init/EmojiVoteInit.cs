using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MongoDB.Driver;
using NdvBot.Database.Mongo;
using NdvBot.Discord.Commands.EmojiVote;
using NdvBot.Discord.Database;
using NdvBot.Utils;

namespace NdvBot.Discord.Init
{
    public class EmojiVoteInit : IInit
    {
        public long Priority { get; set; } = 10;
        private IMongoConnection _mongoConnection;

        
        public const string Tick = "✅";
        public const string Cross = "❌";
        
        private DiscordShardedClient _shardedClient;
        private Timer _timer;
        public Task Init(IServiceProvider serviceProvider)
        {
            var shardedClient = serviceProvider.GetService(typeof(DiscordShardedClient)) as DiscordShardedClient;
            this._shardedClient = shardedClient ?? throw new DataException("Cannot get discord sharded client!");
            this._mongoConnection = serviceProvider.GetService(typeof(IMongoConnection)) as IMongoConnection ??
                                    throw new DataException("Cannot get mongo connection!");

            this._shardedClient.MessageReactionAdded += this.ReactionAdded;
            
            var time = DateTime.Today.StartOfWeek(DayOfWeek.Monday).AddDays(7);
            this._timer = new Timer((time - DateTime.Now).TotalMilliseconds);
            this._timer.AutoReset = true;
            this._timer.Elapsed += this.CalcVote;
            this._timer.Enabled = true;
            return Task.CompletedTask;
        }
        
        private async Task ReactionAdded(DiscordClient client, MessageReactionAddEventArgs args)
        {
            var msg = args.Message;
            if (msg is null) return;
            var reaction = args.Emoji;
            if (reaction is null) return;
            if (args.User.IsBot) return;
            var data = await args.Guild.GetGuildData(this._mongoConnection);
            if (data is null) return;
            
            if (data.EmojiVoteData is null) return;
            if(!data.EmojiVoteData.ReactionMessages.TryGetValue(args.Channel.Id, out var channelMessages)) return;
            if (!channelMessages.Contains(msg.Id)) return;

            var role = await data.EmojiVoteData.GetOrCreateEmojiRole(client, args.Guild);
            var member = await args.Guild.GetMemberAsync(args.User.Id);
            if (member is null) return;
            if (reaction.Name == Tick)
            {
                await member.GrantRoleAsync(role);
            }else if (reaction.Name == Cross)
            {
                await member.RevokeRoleAsync(role);
            }

            await args.Message.DeleteReactionAsync(reaction, args.User);

            var filter = Builders<GuildData>.Filter.Eq("_id", data._id);
            var update = Builders<GuildData>.Update.Set("EmojiVoteData", data.EmojiVoteData);
            await this._mongoConnection.ServerDb.GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                .FindOneAndUpdateAsync(filter, update);
        }

        private async void CalcVote(object sender, ElapsedEventArgs args)
        {
            this._timer.Interval = 604800000;
            try
            {
                var filter = Builders<GuildData>.Filter.Ne<EmojiVoteData?>("EmojiVoteData", null);
                var guildsData = await (await this._mongoConnection.ServerDb
                    .GetCollection<GuildData>(MongoCollections.GuildDataColleciton)
                    .FindAsync(filter)).ToListAsync();
                foreach (var guildData in guildsData)
                {
                    if (guildData.EmojiVoteData.ChannelId is null) continue;
                    var client = this._shardedClient.GetShard(guildData.GuildId);
                    if (client is null) return;
                    var guild = await client.GetGuildAsync(guildData.GuildId);
                    if (guild is null) return;

                    var emotesToDelete = guildData.EmojiVoteData!.EmojiIds;
                    var tasks = new List<Task>();
                    foreach (var emoteToDelete in emotesToDelete)
                    {
                        tasks.Add(guild.DeleteEmojiAsync(
                            (DiscordGuildEmoji) DiscordGuildEmoji.FromGuildEmote(client, emoteToDelete)));
                    }

                    await Task.WhenAll(tasks);

                    var msgToReactionCount = new Dictionary<ulong, long>();
                    var emojiChannel = guild.GetChannel(guildData.EmojiVoteData.ChannelId.Value);
                    if (emojiChannel is null) continue;
                    foreach (var votedMessage in guildData.EmojiVoteData.VotedMessages)
                    {
                        var msg = await emojiChannel.GetMessageAsync(votedMessage);
                        if (msg is null) continue;
                        var tickReactions = msg.Reactions.FirstOrDefault(r => r.Emoji == Tick);
                        if (tickReactions is null)
                        {
                            await msg.DeleteAsync();
                            continue;
                        }
                        msgToReactionCount.Add(msg.Id, tickReactions.Count);
                    }
                    
                    var ordered = msgToReactionCount.OrderByDescending((e) => e.Value).ToList();
                    for(int i = 0; i < 5; i++)
                    {
                        var msgWithImage = ordered[i];
                        var msg = await emojiChannel.GetMessageAsync(msgWithImage.Key);
                        if (msg is null) continue;
                        if (msg.Attachments.Count != 1)
                        {
                            await msg.DeleteAsync();
                            continue;
                        }

                        var emoteUrl = msg.Attachments[0].Url;

                        using var webClient = new WebClient();
                        var data = await webClient.DownloadDataTaskAsync(emoteUrl);
                        var stream = new MemoryStream(data);
                        await guild.CreateEmojiAsync(msg.Attachments[0].FileName, stream);
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

        }

        public Task DeInit()
        {
            throw new NotImplementedException();
        }
    }
}