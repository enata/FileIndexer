namespace FileIndexer.Core.Interfaces
{
    public interface IFileManager
    {
        /// <summary>
        /// Starts observing the directory
        /// </summary>
        /// <param name="directoryPath">path to directory to observe</param>
        void AddDirectory(string directoryPath);

        /// <summary>
        /// Starts oberving the file
        /// </summary>
        /// <param name="filePath">path to file to observe</param>
        void AddFile(string filePath);

        /// <summary>
        /// Stops observing file
        /// </summary>
        /// <param name="filePath">path to file</param>
        /// <returns>true if remove's successful</returns>
        bool TryRemoveFile(string filePath);

        /// <summary>
        /// Stops observing directory
        /// </summary>
        /// <param name="directoryPath">path to directory</param>
        /// <returns>true if remove's successful</returns>
        bool TryRemoveDirectory(string directoryPath);

        /// <summary>
        /// Content of observed file changed
        /// </summary>
        event FileChanged FileChanged;

        /// <summary>
        /// File added to observed directory
        /// </summary>
        event FileChanged FileAdded;

        /// <summary>
        /// Observed file removed
        /// </summary>
        event FileChanged FileRemoved;

        /// <summary>
        /// Observed file renamed
        /// </summary>
        event FileRenamed FileRenamed;
    }

    
}