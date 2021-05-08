using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace NdvBot.Discord.Commands.EmojiManager
{
    public class EmojiManager : BaseCommandModule
    {
        [Command("emojiDownload")]
        [DSharpPlus.CommandsNext.Attributes.Description("Downloads all emojis from server and packs them into archive. Usage: [downloadGifs = true]")]
        public async Task Download(CommandContext ctx, bool downloadGifs = true)
        {
            var emojis = ctx.Guild.Emojis.Where(e => downloadGifs || !e.Value.IsAnimated);
            var emotesData = new ConcurrentDictionary<string, byte[]>();
            var tasks = new List<Task>();
            foreach (var emoji in emojis)
            {
                Task<byte[]> DownloadEmote(string url)
                {
                    using var client = new WebClient();
                    return client.DownloadDataTaskAsync(new Uri(url));
                }
                tasks.Add(DownloadEmote(emoji.Value.Url).ContinueWith(res =>
                {
                    var splitByDot = emoji.Value.Url.Split(".");
                    emotesData.TryAdd(emoji.Value.Name + "." + splitByDot[splitByDot.Length - 1], res.Result);
                }));
            }

            await Task.WhenAll(tasks);
            tasks.Clear();
            
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
            await new DiscordMessageBuilder().WithFiles(
                new Dictionary<string, Stream>() {{"emojis.zip", zipStream}}).SendAsync(ctx.Channel);
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