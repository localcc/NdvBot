using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;

namespace NdvBot.Discord
{
    public class HelpFormatter : DefaultHelpFormatter
    {
        private readonly InteractivityExtension _interactivity;
        private ChunkStringBuilder _chunkBuilder;
        
        public HelpFormatter(CommandContext ctx) : base(ctx)
        {
            this._interactivity = ctx.Client.GetInteractivity();
            this._chunkBuilder = new("asciidoc");
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            StringBuilder localBuilder = new();

            if (command.RunChecksAsync(this.Context, true).GetAwaiter().GetResult().Any()) return this;
            
            localBuilder.Append("[").Append(command.Name).Append("]\n");
            if (command.Description != string.Empty)
            {
                localBuilder.Append("* ").Append(command.Description).Append("\n");
            }

            if (command.Aliases.Count >= 1)
            {
                localBuilder.Append("* [");
                for (var i = 0; i < command.Aliases.Count; i++)
                {
                    localBuilder.Append(command.Aliases[i]);
                    if (i != command.Aliases.Count - 1)
                    {
                        localBuilder.Append(", ");
                    }
                }

                localBuilder.Append("]\n");
            }

            localBuilder.Append("\n");

            this._chunkBuilder.Append(localBuilder);
            return this;
        }


        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            foreach (var subcommand in subcommands)
            {
                this.WithCommand(subcommand);
            }

            return this;
        }

        public override CommandHelpMessage Build()
        {
            this._chunkBuilder.PrintOut((e) => this.Context.Channel.SendMessageAsync(e.ToString())).GetAwaiter().GetResult();
            return new CommandHelpMessage();
        }
    }
}