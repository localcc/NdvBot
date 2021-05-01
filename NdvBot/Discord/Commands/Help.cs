using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace NdvBot.Discord.Commands
{
    public class Help : ModuleBase<CustomCommandContext>
    {
        private readonly CommandService _commandService;
        private readonly IServiceProvider _serviceProvider;
        public Help(IServiceProvider serviceProvider, CommandService commandService)
        {
            this._serviceProvider = serviceProvider;
            this._commandService = commandService;
        }

        [Command("help")]
        [Summary("Lists all commands")]
        public async Task HelpCommand()
        {
            List<StringBuilder> builders = new();
            StringBuilder curBuilder = new();
            curBuilder.Append("```asciidoc\n");
            foreach (var command in this._commandService.Commands.Where(c =>
            {
                foreach (var precondition in c.Preconditions)
                {
                    var res = precondition.CheckPermissionsAsync(this.Context, c, this._serviceProvider).GetAwaiter().GetResult();
                    if (!res.IsSuccess) return false;
                }

                return true;
            }))
            {
                StringBuilder localBuilder = new();
                StringBuilder subBuilder = new();
                localBuilder.Append("[");
                localBuilder.Append(command.Name);
                localBuilder.Append("]\n");
                
                if (command.Summary != string.Empty)
                {
                    localBuilder.Append("* ");
                    localBuilder.Append(command.Summary);
                    localBuilder.Append("\n");
                }
                
                if (command.Aliases.Count > 1)
                {
                    localBuilder.Append("* `[");
                    for (var i = 1; i < command.Aliases.Count; i++)
                    {
                        subBuilder.Append(command.Aliases[i]);
                        subBuilder.Append(", ");
                    }

                    var subBuilderRes = subBuilder.ToString();
                    localBuilder.Append(subBuilderRes.Substring(0, subBuilderRes.Length - 2));
                    subBuilder.Clear();
                    localBuilder.Append("]'\n");
                }

                if (command.Parameters.Count > 0)
                {
                    subBuilder.Append("* Arguments: \n");
                    foreach (var parameter in command.Parameters)
                    {
                        subBuilder.Append("    ");
                        subBuilder.Append(parameter.Type.Name);
                        subBuilder.Append(":: ");
                        subBuilder.Append(parameter.Name);
                        subBuilder.Append("\n");
                    }

                    localBuilder.Append(subBuilder);
                    subBuilder.Clear();
                }

                localBuilder.Append("\n");
                if (localBuilder.Length + curBuilder.Length >= 2000)
                {
                    builders.Add(curBuilder);
                    curBuilder = new();
                }
                curBuilder.Append(localBuilder);
            }

            curBuilder.Append("\n```");
            
            foreach (var builder in builders)
            {
                await ReplyAsync(builder.ToString());
            }

            await ReplyAsync(curBuilder.ToString());
        }
    }
}