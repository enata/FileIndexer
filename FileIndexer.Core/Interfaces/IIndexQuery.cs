using System.Collections.Generic;

namespace FileIndexer.Core.Interfaces
{
    public interface IIndexQuery
    {
        IEnumerable<string> Execute(IReadOnlyDictionary<IWord, HashSet<string>> wordIndex);
    }
}