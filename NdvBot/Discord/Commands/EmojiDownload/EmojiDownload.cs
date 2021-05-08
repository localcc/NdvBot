using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NdvBot.Discord.Commands.EmojiDownload
{
//     public class EmojiDownload : ModuleBase<CustomCommandContext>
//     {
//         [Command("emojiDownload", RunMode = RunMode.Async)]
//         [Summary("Downloads all emojis from server, and packs them into archive")]
//         public async Task<RuntimeResult> Download(bool downloadGifs = true)
//         {
//             var emotes = Context.Guild.Emotes.Where(e => downloadGifs || e.Url.EndsWith(".gif"));
//             
//             var emotesData = new ConcurrentDictionary<string, byte[]>();
//             var tasks = new List<Task>();
//             foreach (var emote in emotes)
//             {
//                 Task<byte[]> DownloadEmote(string url)
//                 {
//                     using var client = new WebClient();
//                     return client.DownloadDataTaskAsync(new Uri(url));
//                 }
//                 tasks.Add(DownloadEmote(emote.Url).ContinueWith(res =>
//                 {
//                     var splitByDot = emote.Url.Split(".");
//                     emotesData.TryAdd(emote.Name + "." + splitByDot[splitByDot.Length - 1], res.Result);
//                 }));
//             }
//
//             await Task.WhenAll(tasks);
//             tasks.Clear();
//             
//             await using var zipStream = new MemoryStream();
//             using var zipArchive = new ZipArchive(zipStream, ZipArchiveMode.Create, true);
//             foreach (var emoteData in emotesData)
//             {
//                 await using var emoteContentStream = new MemoryStream(emoteData.Value);
//                 await using var zipEntryStream = zipArchive.CreateEntry(emoteData.Key).Open();
//                 await emoteContentStream.CopyToAsync(zipEntryStream);
//             }
//             zipArchive.Dispose();
//
//
//             await File.WriteAllBytesAsync("test.zip", zipStream.ToArray());
//             await this.Context.Channel.SendFileAsync("test.zip");
// //            await this.Context.Channel.SendFileAsync(zipStream, "emotes.zip");
//             return CommandResult.FromSuccess();
//         }
    // }
}