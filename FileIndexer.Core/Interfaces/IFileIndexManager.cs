using System.Collections.Generic;
using System.IO;

namespace FileIndexer.Core.Interfaces
{
    public interface IFileIndexManager
    {
        /// <summary>
        /// Adds files from the directories to the current index
        /// </summary>
        /// <param name="directoryPaths">paths to directories</param>
        void AddDirectories(params string[] directoryPaths);

        /// <summary>
        /// Adds files to the current index
        /// </summary>
        /// <param name="filePaths">paths to fiies</param>
        void AddFiles(params string[] filePaths);

        /// <summary>
        /// Removes file from the current index if possible
        /// </summary>
        /// <param name="filePath">path to the file</param>
        /// <returns>true if remove's successful</returns>
        bool TryRemoveFile(string filePath);

        /// <summary>
        /// Removes files from the directory if possible
        /// </summary>
        /// <param name="directoryPath">path to the directory</param>
        /// <returns>true if remove's successful</returns>
        bool TryRemoveDirectory(string directoryPath);

        /// <summary>
        /// Executes provided query on the local index
        /// </summary>
        /// <param name="query">query to execute</param>
        /// <returns>query execution result</returns>
        IEnumerable<FileInfo> QueryIndex(IIndexQuery query);
    }
}