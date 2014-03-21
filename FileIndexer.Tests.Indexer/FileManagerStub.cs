using System;
using System.Collections.Generic;
using System.Linq;
using FileIndexer.Core;
using FileIndexer.Core.Interfaces;

namespace FileIndexer.Tests.Indexer
{
    internal sealed class FileManagerStub : IFileManager
    {
        public FileManagerStub(IEnumerable<string> directories, IEnumerable<string> files)
        {
            Directories = new HashSet<string>(directories);
            Files = new HashSet<string>(files);
        }

        public FileManagerStub()
            : this(Enumerable.Empty<String>(), Enumerable.Empty<string>())
        {
        }

        public HashSet<string> Directories { get; private set; }
        public HashSet<string> Files { get; private set; }

        public void AddDirectory(string directoryPath)
        {
            Directories.Add(directoryPath);
        }

        public void AddFile(string filePath)
        {
            Files.Add(filePath);
        }

        public bool TryRemoveFile(string filePath)
        {
            Files.Remove(filePath);
            return true;
        }

        public bool TryRemoveDirectory(string directoryPath)
        {
            Directories.Remove(directoryPath);
            return true;
        }

        public void FireFileAdded(string fileName)
        {
            var handlers = FileAdded;
            if (handlers != null)
                handlers(fileName);
        }

        public void FireFileRemoved(string fileName)
        {
            var handlers = FileRemoved;
            if (handlers != null)
                handlers(fileName);
        }

        public void FireFileChanged(string fileName)
        {
            var handlers = FileChanged;
            if (handlers != null)
                handlers(fileName);
        }

        public void FireFileRenamed(string oldFileName, string newFileName)
        {
            var handlers = FileRenamed;
            if (handlers != null)
                handlers(oldFileName, newFileName);
        }

        public event FileChanged FileChanged;
        public event FileChanged FileAdded;
        public event FileChanged FileRemoved;
        public event FileRenamed FileRenamed;
    }
}