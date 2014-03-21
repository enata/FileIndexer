using FileIndexer.Core.Interfaces;
using System.IO;

namespace FileIndexer.Indexer
{
    public sealed class TextLoader : ITextLoader
    {
        public string LoadText(string sourcePath)
        {
            string result = File.ReadAllText(sourcePath); // TODO: Encoding!
            return result;
        }
    }
}