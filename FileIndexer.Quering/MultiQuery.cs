using System;
using System.Collections.Generic;
using System.Linq;
using FileIndexer.Core.Interfaces;

namespace FileIndexer.Quering
{
    public abstract class MultiQuery : IIndexQuery
    {
        private readonly IIndexQuery[] _queries;

        protected MultiQuery(params IIndexQuery[] queries)
        {
            if (queries == null) throw new ArgumentNullException("queries");

            _queries = queries;
        }

        public IEnumerable<string> Execute(IReadOnlyDictionary<IWord, HashSet<string>> wordIndex)
        {
            if (wordIndex == null) throw new ArgumentNullException("wordIndex");

            if (!_queries.Any())
                return Enumerable.Empty<string>();

            var queriesResult = _queries.AsParallel()
                                        .Select(q => new HashSet<string>(q.Execute(wordIndex)))
                                        .ToArray();
            var result = UniteResult(queriesResult);

            return result;
        }

        protected abstract IEnumerable<string> UniteResult(HashSet<string>[] subQueriesResults);

    }
}