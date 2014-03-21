using FileIndexer.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileIndexer.Indexer
{
    public sealed class FileIndexManager : IFileIndexManager
    {
        private readonly IFileManager _fileManager;
        private readonly ILogger _logger;
        private readonly IFileIndex _index;

        public FileIndexManager(IFileManager fileManager, ILogger logger, IFileIndexFactory fileIndexFactory)
        {
            if (fileManager == null) throw new ArgumentNullException("fileManager");
            if (logger == null) throw new ArgumentNullException("logger");
            if (fileIndexFactory == null) throw new ArgumentNullException("fileIndexFactory");

            _fileManager = fileManager;
            _logger = logger;
            _index = fileIndexFactory.BuildEmptyIndex(fileManager);
        }

        public void AddDirectories(params string[] directoryPaths)
        {
            if (directoryPaths == null) throw new ArgumentNullException("directoryPaths");

            foreach (var directoryPath in directoryPaths)
            {
                try
                {
                    _fileManager.AddDirectory(directoryPath);
                }
                catch (Exception ex)
                {
                    _logger.Log(ex.Message);
                }
            }
            
        }

        public void AddFiles(params string[] filePaths)
        {
            if (filePaths == null) throw new ArgumentNullException("filePaths");

            foreach (var filePath in filePaths)
            {
                try
                {
                    _fileManager.AddFile(filePath);
                }
                catch (Exception ex)
                {
                    _logger.Log(ex.Message);
                }
            }
            
        }

        public IEnumerable<FileInfo> QueryIndex(IIndexQuery query)
        {
            if (query == null) throw new ArgumentNullException("query");

            var filePaths = _index.ExecuteQuery(query).ToList();
            var result = new List<FileInfo>(filePaths.Count);
            foreach (var filePath in filePaths)
            {
                try
                {
                    result.Add(new FileInfo(filePath));
                }
                catch (Exception ex)
                {
                    _logger.Log(ex.Message);
                }
            }
            return result;
        }

        public bool TryRemoveFile(string filePath)
        {
            if(string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException();

            return _fileManager.TryRemoveFile(filePath);
        }

        public bool TryRemoveDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath)) throw new ArgumentException();

            return _fileManager.TryRemoveDirectory(directoryPath);
        }
    }
}