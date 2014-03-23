using System;
using FileIndexer.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace FileIndexer.FileSystemManagement
{
    /// <summary>
    /// Observes changes of files and subdirectories in some directory and notifies about them
    /// </summary>
    internal sealed class DirectoryObservation
    {
        

        private const string TxtExtension = "*.txt"; // TODO: move into settings

        private readonly FileSystemWatcher _fileWatcher; // monitors files changes
        private readonly FileSystemWatcher _directoryWatcher; // monitors subdirectories changes

        private readonly HashSet<string> _files = new HashSet<string>();

        private readonly ReaderWriterLockSlim _globalSynLock; // to synchronize file tree global changes
        private readonly object _syncRoot = new object(); // to keep local integrity

        public DirectoryObservation(string directoryPath, ReaderWriterLockSlim globalSynLock)
        {
            _globalSynLock = globalSynLock;
            _fileWatcher = CreateFileWatcher(directoryPath, NotifyFilters.FileName|NotifyFilters.LastWrite);
            _fileWatcher.Filter = TxtExtension;
            _directoryWatcher = CreateFileWatcher(directoryPath, NotifyFilters.DirectoryName);
            
            AddWatchingEvents(_fileWatcher);
            ScanDirectory(directoryPath);
        }

        public event FileChanged FileChanged;
        public event FileChanged FileAdded;
        public event FileChanged FileRemoved;
        public event FileRenamed FileRenamed;
        public event FileChanged DirectoryRemoved;
        public event FileRenamed DirectoryRenamed;

        public void ForceRenameEvent(string oldName, string newName)
        {
            if(string.Equals(oldName, newName))
                return;

            lock (_syncRoot)
            {
                _fileWatcher.Path = _directoryWatcher.Path = _fileWatcher.Path.ToLower().Replace(oldName, newName);
                foreach (var file in _files.ToArray())
                {
                    OnFileRenamed(file, file.Replace(oldName, newName));
                }
            }
        }

        public void ForceRemoveEvent()
        {
            lock (_syncRoot)
            {
                foreach (var file in _files)
                {
                    OnFileDeleted(file, false);
                }
            }
        }

        public void ForceAdddEvent()
        {
            lock (_syncRoot)
            {
                foreach (var file in _files)
                {
                    OnFileAdded(file, false);
                }
            }
        }

        private void ScanDirectory(string path)
        {
            var files = Directory.GetFiles(path, TxtExtension, SearchOption.TopDirectoryOnly);
            lock (_syncRoot)
            {
                foreach (var file in files)
                {
                    OnFileAdded(file.ToLower());
                }
            }
        }

        private FileSystemWatcher CreateFileWatcher(string path, NotifyFilters notifyFilters)
        {
            var result = new FileSystemWatcher
                {
                    Path = path,
                    IncludeSubdirectories = false,
                    NotifyFilter = notifyFilters,
                    EnableRaisingEvents = true
                };
            return result;
        }

        private void AddWatchingEvents(FileSystemWatcher fileWatcher)
        {
            fileWatcher.Created += FileWatcherOnCreated;
            fileWatcher.Deleted += FileWatcherOnDeleted;
            fileWatcher.Changed += FileWatcherOnChanged;
            fileWatcher.Renamed += FileWatcherOnRenamed;
            _directoryWatcher.Deleted += DirectoryWatcherOnDeleted;
            _directoryWatcher.Renamed += DirectoryWatcherOnRenamed;
        }
        

        private void DirectoryWatcherOnRenamed(object sender, RenamedEventArgs renamedEventArgs)
        {
            _globalSynLock.EnterWriteLock();
            var dirRenamedHandlers = DirectoryRenamed;
            if (dirRenamedHandlers != null)
            {
                dirRenamedHandlers(renamedEventArgs.OldName.ToLower(), renamedEventArgs.Name.ToLower());
            }
            _globalSynLock.ExitWriteLock();
        }

        private void DirectoryWatcherOnDeleted(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            _globalSynLock.EnterReadLock();
            var dirRemovedHandlers = DirectoryRemoved;
            if(dirRemovedHandlers != null)
            {
                dirRemovedHandlers(fileSystemEventArgs.Name);
            }
            _globalSynLock.ExitReadLock();
        }

        private void FileWatcherOnCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            _globalSynLock.EnterReadLock();
            OnFileAdded(fileSystemEventArgs.FullPath.ToLower());
            _globalSynLock.ExitReadLock();
        }

        private void OnFileAdded(string fullPath, bool addToLocalSet = true)
        {
            var fileAddedHandlers = FileAdded;

            var normalizedFilePath = fullPath.ToLower();
            if (addToLocalSet)
                _files.Add(normalizedFilePath);
            if (fileAddedHandlers != null)
            {
                fileAddedHandlers(normalizedFilePath);
            }
        }

        private void FileWatcherOnDeleted(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            _globalSynLock.EnterReadLock();
            var path = ReplacePathIfChanged(fileSystemEventArgs.FullPath.ToLower());
            OnFileDeleted(path);
            _globalSynLock.ExitReadLock();
            
        }

        private void OnFileDeleted(string fullPath, bool deleteFromLocalSet = true)
        {
            var normalizedFilePath = fullPath.ToLower();
            if(deleteFromLocalSet)
                _files.Remove(normalizedFilePath);
            var fileDeletedHandler = FileRemoved;
            if (fileDeletedHandler != null)
            {
                fileDeletedHandler(normalizedFilePath);
            }

        }

        private void FileWatcherOnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            _globalSynLock.EnterReadLock();
            var changeHandlers = FileChanged;
            if (changeHandlers != null)
            {
                
                var normalizedFilePath = ReplacePathIfChanged(fileSystemEventArgs.FullPath.ToLower());
                changeHandlers(normalizedFilePath);
            }
            _globalSynLock.ExitReadLock();
        }

        private string ReplacePathIfChanged(string originalPath)
        {
            var currentFolderPath = _directoryWatcher.Path.ToLower();
            if (originalPath.StartsWith(currentFolderPath))
                return originalPath;
            return ReplacePath(originalPath, currentFolderPath);
        }

        private string ReplacePath(string originalPath, string updatedPathPart)
        {
            int diverseIndex = FindDiverseIndex(originalPath, updatedPathPart);
            int replaceBorder = originalPath.IndexOf('\\', diverseIndex);
            var result = replaceBorder >= 0 ? updatedPathPart + originalPath.Substring(replaceBorder) : updatedPathPart;
            return result;
        }

        private int FindDiverseIndex(string str1, string str2)
        {
            for (int i = 0; i < str1.Length; i++)
            {
                if (str1[i] != str2[i])
                    return i;
            }
            return -1;
        }

        private void FileWatcherOnRenamed(object sender, RenamedEventArgs renamedEventArgs)
        {
            
            _globalSynLock.EnterReadLock();
            var path = ReplacePathIfChanged(renamedEventArgs.OldFullPath.ToLower());
            OnFileRenamed(path, renamedEventArgs.FullPath);
            _globalSynLock.ExitReadLock();
        }

        private void OnFileRenamed(string oldFullPath, string newFullPath)
        {
            var normalizedOldName = oldFullPath.ToLower();
            var normalizedNewName = newFullPath.ToLower();
            _files.Remove(normalizedOldName);
            _files.Add(normalizedNewName);
            var renameHandlers = FileRenamed;
            if (renameHandlers != null)
            {
                renameHandlers(normalizedOldName, normalizedNewName);
            }
        }
    }

    internal sealed class DirectoryObservationFactory
    {
        private readonly ReaderWriterLockSlim _globalSyncLock = new ReaderWriterLockSlim();

        public DirectoryObservation Produce(string directoryPath)
        {
            return new DirectoryObservation(directoryPath, _globalSyncLock);
        }
    }
}