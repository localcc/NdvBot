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

        public bool Append(StringBuilder builder, int limit = 2000)
        {
            if (builder.Length >= limit)
            {
                return false;
            }
            if (builder.Length + this._builder.Length >= limit)
            {
                this._builder.Append("\n```");
                this._builders.Add(this._builder);
                this._builder = new();
                ChunkStringBuilder.AppendSyntax(this._builder, this._syntax);
            }

            this._builder.Append(builder);
            return true;
        }

        public bool Append(string str, int limit = 2000)
        {
            if (str.Length >= limit)
            {
                return false;
            }

            if (str.Length + this._builder.Length >= limit)
            {
                this._builder.Append("\n```");
                this._builders.Add(this._builder);
                this._builder = new();
                ChunkStringBuilder.AppendSyntax(this._builder, this._syntax);
            }

            this._builder.Append(str);
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

        private IEnumerable<StringBuilder> AsEnumerable()
        {
            var bCopy = this._builder;
            bCopy.Append("\n```");
            yield return bCopy;
            for (int i = 0; i < this._builders.Count; i++)
            {
                yield return this._builders[i];
            }
        }

        public StringBuilder AsOne(int limit = 0)
        {
            StringBuilder builder = new();
            foreach (var stringBuilder in this.AsEnumerable())
            {
                if (limit != 0 && builder.Length + stringBuilder.Length > limit)
                {
                    var bString = stringBuilder.ToString();
                    builder.Append(bString.Substring(0, bString.Length - 7)); // seven for ...\n```
                    builder.Append("...\n```");
                    return builder;
                } 
                builder.Append(stringBuilder);
            }

            return builder;
        }
    }
}