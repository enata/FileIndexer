namespace FileIndexer.Core.Interfaces
{
    public interface IFileIndexFactory
    {
        IFileIndex BuildEmptyIndex(IFileManager manager);
    }
}