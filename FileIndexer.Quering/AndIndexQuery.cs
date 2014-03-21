using System.Collections.Generic;
using FileIndexer.Core.Interfaces;

namespace FileIndexer.Quering
{
    public sealed class AndIndexQuery : MultiQuery
    {
        public AndIndexQuery(params IIndexQuery[] queries) : base(queries)
        {}

        protected override IEnumerable<string> UniteResult(HashSet<string>[] subQueriesResults)
        {
            var result = subQueriesResults[0];
            for (int i = 1; i < subQueriesResults.Length; i++)
            {
                result.IntersectWith(subQueriesResults[i]);
            }

            return result;
        }
    }
}