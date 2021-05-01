using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NdvBot.Discord
{
    public class ChunkStringBuilder
    {
        private List<StringBuilder> _builders = new();
        private StringBuilder _builder = new();
        private readonly string _syntax;
        
        public ChunkStringBuilder(string syntax)
        {
            this._syntax = syntax;
            ChunkStringBuilder.AppendSyntax(this._builder, syntax);
        }

        private static void AppendSyntax(StringBuilder builder, string syntax)
        {
            builder.Append("```");
            builder.Append(syntax);
            builder.Append("\n");
        }

        public bool Append(StringBuilder builder)
        {
            if (builder.Length >= 2000)
            {
                return false;
            }
            if (builder.Length + this._builder.Length >= 2000)
            {
                this._builder.Append("\n```");
                this._builder.Append(_builder);
                this._builder = new();
                ChunkStringBuilder.AppendSyntax(this._builder, this._syntax);
            }

            this._builder.Append(builder);
            return true;
        }

        public async Task PrintOut(Func<StringBuilder, Task> printFunc)
        {
            foreach (var builder in this._builders)
            {
                await printFunc(builder);
            }

            this._builder.Append("\n```");
            await printFunc(this._builder);
        }
    }
}