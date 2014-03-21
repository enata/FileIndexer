using FileIndexer.Core;
using FileIndexer.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FileIndexer.Quering
{
    public sealed class HasIndexQuery : IIndexQuery
    {
        private readonly IWord _word;

        public HasIndexQuery(string word)
        {
            if (word == null) throw new ArgumentNullException("word");

            _word = new Word(word);
        }

        public IEnumerable<string> Execute(IReadOnlyDictionary<IWord, HashSet<string>> wordIndex)
        {
            if (wordIndex == null) throw new ArgumentNullException("wordIndex");

            HashSet<string> result;
            return wordIndex.TryGetValue(_word, out result) ? result : Enumerable.Empty<string>();
        }
    }
}