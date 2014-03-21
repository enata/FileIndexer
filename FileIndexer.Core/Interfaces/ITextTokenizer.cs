using System.Collections.Generic;

namespace FileIndexer.Core.Interfaces
{
    // Required extension point. Implement this interface to introduce the way of breaking text into words
    public interface ITextTokenizer
    {
        /// <summary>
        /// Breakes text into tokens
        /// </summary>
        /// <param name="text">text to tokenize</param>
        /// <returns>sequence of tokens</returns>
        IEnumerable<IWord> Tokenize(string text);
    }
}