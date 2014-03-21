using FileIndexer.Core.Interfaces;

namespace FileIndexer.Indexer
{
    public sealed class FileIndexFactory : IFileIndexFactory
    {
        private readonly ITextTokenizer _tokenizer;
        private readonly ITextLoader _loader;
        private readonly ILogger _logger;

        public FileIndexFactory(ITextTokenizer tokenizer, ITextLoader loader, ILogger logger)
        {
            _tokenizer = tokenizer;
            _loader = loader;
            _logger = logger;
        }

        public IFileIndex BuildEmptyIndex(IFileManager manager)
        {
            return new FileIndex(manager, _tokenizer, _loader, _logger);
        }
    }
}