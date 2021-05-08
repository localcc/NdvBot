using System;
using System.Text;

namespace NdvBot.Utils
{
    public class StringProgressBar
    {
        private readonly StringBuilder _builder;
        private readonly string _prefix;
        private readonly string _postfix;

        private readonly int _effectiveLength;
        private readonly char _progressSymbol;
        
        private double _progress;

        public StringProgressBar(int progressBarLength, string prefix, string postfix, char progressSymbol)
        {
            this._builder = new();
            this._effectiveLength = progressBarLength - prefix.Length - postfix.Length;
            this._prefix = prefix;
            this._postfix = postfix;
            this._progressSymbol = progressSymbol;
        }

        public void SetProgress(int progress)
        {
            this._progress = progress / 100d;
        }

        public override string ToString()
        {
            this._builder.Clear();
            this.FillBuilder(this._builder);
            return this._builder.ToString();
        }

        public StringBuilder ToStringBuilder()
        {
            var builder = new StringBuilder();
            this.FillBuilder(builder);
            return builder;
        }
        
        private void FillBuilder(StringBuilder builder)
        {
            builder.Append(this._prefix);
            var progressSymbols = (int)Math.Round(this._progress * this._effectiveLength);
            builder.Append(new string(this._progressSymbol, progressSymbols));
            builder.Append(new string(' ', this._effectiveLength - progressSymbols));
            builder.Append(this._postfix);
        }
    }
}