using System.Collections.Generic;

namespace FileIndexer.Core.Interfaces
{
    public interface IFileIndex
    {
        /// <summary>
        /// Executes provided query
        /// </summary>
        /// <param name="query">query to execute</param>
        /// <returns>file names resulting from the query</returns>
        IEnumerable<string> ExecuteQuery(IIndexQuery query);
    }
}