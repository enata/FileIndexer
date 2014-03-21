using FileIndexer.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        //TODO: introduce global synchronization point, to synchronize events propagated through file tree structure
        private readonly object _syncRoot = new object();

        public DirectoryObservation(string directoryPath)
        {
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
            var dirRenamedHandlers = DirectoryRenamed;
            if(dirRenamedHandlers != null)
                lock (_syncRoot)
                {
                    dirRenamedHandlers(renamedEventArgs.OldName, renamedEventArgs.Name);
                }
        }

        private void DirectoryWatcherOnDeleted(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            var dirRemovedHandlers = DirectoryRemoved;
            if(dirRemovedHandlers != null)
                lock (_syncRoot)
                {
                    dirRemovedHandlers(fileSystemEventArgs.Name);
                }
        }

        private void FileWatcherOnCreated(object sender, FileSystemEventArgs fileSystemEventArgs)
        {
            lock (_syncRoot)
            {
                OnFileAdded(fileSystemEventArgs.FullPath.ToLower());
            }
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
            lock (_syncRoot)
            {
                OnFileDeleted(fileSystemEventArgs.FullPath);
            }
            
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
            var changeHandlers = FileChanged;
            if (changeHandlers != null)
            {
                var normalizedFilePath = fileSystemEventArgs.FullPath.ToLower();
                lock (_syncRoot)
                {
                    changeHandlers(normalizedFilePath);
                }
            }
        }

        private void FileWatcherOnRenamed(object sender, RenamedEventArgs renamedEventArgs)
        {
            
            lock (_syncRoot)
            {
                OnFileRenamed(renamedEventArgs.OldFullPath, renamedEventArgs.FullPath);
            }
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
}