namespace FileIndexer.Core
{
    public delegate void FileChanged(string filePath);

    public delegate void FileRenamed(string oldPath, string newPath);
}