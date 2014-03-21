namespace FileIndexer.Core.Interfaces
{
    public interface IIndexPreserver
    {
        void Save(IFileIndex index);
        IFileIndex Load(string path);
    }
}