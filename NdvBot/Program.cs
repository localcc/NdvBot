using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NdvBot.Config;
using NdvBot.Database.Mongo;
using NdvBot.Discord;

namespace NdvBot
{
    class Program
    {
        
        static async Task<int> Main(string[] args)
        {
            if (args.Length != 2)
            {
                await Console.Error.WriteLineAsync("Invalid arguments!\nUsage: [CONFIG_FILE] [CERTS_PATH]");
                return -1;
            }

            var fileInfo = new FileInfo(args[0]);
            if (!fileInfo.Exists)
            {
                await Console.Error.WriteLineAsync("Config file doesn't exist!");
                return -2;
            }

            var content = await File.ReadAllTextAsync(args[0]);
            try
            {
                var deserialized = JsonSerializer.Deserialize<ConfigFile>(content);
                if (deserialized is null)
                {
                    await Console.Error.WriteLineAsync("Failed to parse config file!");
                    return -3;
                }

                ConfigFile.Current = deserialized;
                var discordClient = new DiscordShardedClient(new DiscordConfiguration
                {
                    Token = ConfigFile.Current.Token,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.All,
                    MinimumLogLevel = LogLevel.Trace
                });
                var serviceProvider = new ServiceCollection().AddSingleton(discordClient)
                    .AddSingleton<IMongoConnection, MongoConnection>((provider) => new MongoConnection(args[1])).AddSingleton<IClient, Client>()
                    .BuildServiceProvider();
                var client = (IClient) serviceProvider.GetService(typeof(IClient));
                await client.Start(deserialized.Token);
            }
            catch (JsonException)
            {
                await Console.Error.WriteLineAsync("Failed to parse config file!");
                return -3;
            }

            await Task.Delay(-1);
            return 0;
        }

    }
}