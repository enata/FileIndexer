using FileIndexer.Core;
using FileIndexer.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FileIndexer.TextProcessing
{
    public sealed class SimpleRegexTextTokenizer : ITextTokenizer
    {
        private const string WordExpression = @"(\w|\d)+";
        private readonly Regex _wordRegex = new Regex(WordExpression, RegexOptions.Compiled);

        public IEnumerable<IWord> Tokenize(string text)
        {
            if (text == null) throw new ArgumentNullException("text");

            var matches = _wordRegex.Matches(text);
            return matches.Cast<Match>()
                          .Select(match => new Word(match.Value.ToLower()));
        }
    }
}