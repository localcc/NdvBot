using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using NdvBot.Utils;

namespace NdvBot.Discord.Commands.EmojiManager
{
    public class EmojiManager : BaseCommandModule
    {

        private static string GetUrlForProgress(ulong emojiId, string emojiName)
        {
            return emojiId + "_" + emojiName;
        }
        
        [Command("emojiDownload")]
        [DSharpPlus.CommandsNext.Attributes.Description("Downloads all emojis from server and packs them into archive. Usage: [downloadGifs = true]")]
        public async Task Download(CommandContext ctx, bool downloadGifs = true)
        {
            var emojis = ctx.Guild.Emojis.Where(e => downloadGifs || !e.Value.IsAnimated);
            if (ctx.Guild.Emojis.Count <= 0)
            {
                return;
            }

            var downloadProgress = new ConcurrentDictionary<string, int>();
            var emojisList = emojis.ToList();
            foreach (var (_, emoji) in emojisList)
            {
                downloadProgress.TryAdd(EmojiManager.GetUrlForProgress(emoji.Id, emoji.Name), 0);
            }

            var msg = await ctx.RespondAsync("Download progress: ");        
            var progressTimer = new Timer(1000); // because of discord ratelimit
            var stringProgressBar = new StringProgressBar(50, "Download progress: [", "]", '#');
            progressTimer.Enabled = true;
            progressTimer.AutoReset = true;
            progressTimer.Elapsed += async (_, _) =>
            {
                stringProgressBar.SetProgress(downloadProgress.Sum(e => e.Value) / downloadProgress.Count);
                await msg.ModifyAsync(stringProgressBar.ToString());
            };
            progressTimer.Start();

            var emotesData = new ConcurrentDictionary<string, byte[]>();
            var tasks = new List<Task>();
            foreach (var emoji in emojisList)
            {
                Task<byte[]> DownloadEmote(string url, string urlForProgress)
                {
                    using var client = new WebClient();
                    client.DownloadProgressChanged += (o, args) =>
                    {
                        if (!downloadProgress.TryGetValue(urlForProgress, out var progress))
                        {
                            downloadProgress.TryAdd(urlForProgress, args.ProgressPercentage);
                        }
                        else
                        {
                            downloadProgress.TryUpdate(urlForProgress, args.ProgressPercentage, progress);
                        }
                    };
                    return client.DownloadDataTaskAsync(new Uri(url));
                }
                tasks.Add(DownloadEmote(emoji.Value.Url, EmojiManager.GetUrlForProgress(emoji.Value.Id, emoji.Value.Name)).ContinueWith(res =>
                {
                    var splitByDot = emoji.Value.Url.Split(".");
                    emotesData.TryAdd(emoji.Value.Name + "." + splitByDot[splitByDot.Length - 1], res.Result);
                }));
            }
            await Task.WhenAll(tasks);
            tasks.Clear();
            progressTimer.Stop();
            
            await using var zipStream = new MemoryStream();
            using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true);
            foreach (var emoteData in emotesData)
            {
                await using var emoteContentStream = new MemoryStream(emoteData.Value);
                await using var zipEntryStream = zipArchive.CreateEntry(emoteData.Key).Open();
                await emoteContentStream.CopyToAsync(zipEntryStream);
            }
            zipArchive.Dispose();


            zipStream.Position = 0;
            var builder =  new DiscordMessageBuilder().WithFiles(
                new Dictionary<string, Stream>() {{"emojis.zip", zipStream}});
            var t1 =  msg.DeleteAsync();
            var t2 = ctx.RespondAsync(builder);
            await Task.WhenAll(t1, t2);
        }

        [Command("deleteEmojis")]
        [RequireGuild]
        [RequireUserPermissions(Permissions.Administrator)]
        [RequireBotPermissions(Permissions.ManageEmojis)]
        [DSharpPlus.CommandsNext.Attributes.Description(
            "Deletes all emojis from the server. Usage: [deleteGifs = true]")]
        public async Task DeleteEmojis(CommandContext ctx, bool deleteGifs = true)
        {
            var tasks = new List<Task>();
            foreach (var (_, emoji) in ctx.Guild.Emojis.Where(e => deleteGifs || !e.Value.IsAnimated))
            {
                tasks.Add(ctx.Guild.DeleteEmojiAsync((DiscordGuildEmoji)emoji));
            }

            await Task.WhenAll(tasks);
        }
    }
}