using FileIndexer.Core;
using FileIndexer.Core.Interfaces;
using System;
using System.IO;

namespace FileIndexer.FileSystemManagement
{
    public sealed class FileTree :IFileManager
    {
        private readonly FileSystemNode _root = new FileSystemNode(string.Empty, false);
        private readonly object _syncRoot = new object();

        public FileTree()
        {
            // All file events are propagated to the root, subscribe to them
            _root.FileAdded += (path, monitored) => RootFileChangedMonitored(path, monitored, FileAdded);
            _root.FileChanged += (path, monitored) => RootFileChangedMonitored(path, monitored, FileChanged);
            _root.FileRemoved += (path, monitored) => RootFileChangedMonitored(path, monitored, FileRemoved);
            _root.FileRenamed += RootOnFileRenamed;
        }

        public event FileChanged FileChanged;
        public event FileChanged FileAdded;
        public event FileChanged FileRemoved;
        public event FileRenamed FileRenamed;

        public void AddDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException();

            var normalizedPath = path.ToLower();
            lock (_syncRoot)
            {
                FileSystemNode destination;
                TryReachPath(normalizedPath, true, out destination);
                destination.MonitorFolder = true;
                BuildInnerFolders(normalizedPath, destination);
            }
        }

        public void AddFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException();

            var fileDirectory = GetFileDirectory(filePath.ToLower());
            lock (_syncRoot)
            {
                FileSystemNode parentDirectory;
                TryReachPath(fileDirectory, true, out parentDirectory);
                parentDirectory.MonitorFile(filePath);
            }
        }

        public bool TryRemoveDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath)) throw new ArgumentException();

            var normalizedPath = directoryPath.ToLower();
            FileSystemNode directoryToRemove;
            lock (_syncRoot)
            {

                if (!TryReachPath(normalizedPath, false, out directoryToRemove))
                    return false;
            }

            //It might be better not to remove the node physically : file system structure is kind of cached this way
            directoryToRemove.MonitorFolder = false;
            return true;
        }

        public bool TryRemoveFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException();

            var normalizedPath = filePath.ToLower();
            var fileDirectory = GetFileDirectory(normalizedPath);
            lock (_syncRoot)
            {
                FileSystemNode parentDirectory;
                if (!TryReachPath(fileDirectory, false, out parentDirectory))
                    return false;
                return parentDirectory.StopMonitoringFile(normalizedPath);
            }
        }

        private static string GetFileDirectory(string filePath)
        {
            var fileSeparatorPos = filePath.LastIndexOf(Path.DirectorySeparatorChar);
            var fileDirectory = filePath.Substring(0, fileSeparatorPos);
            return fileDirectory;
        }


        // Pass outwards only monitored files messages

        private void RootOnFileRenamed(string oldPath, string newPath, bool monitored)
        {
            if (!monitored)
                return;
            var handlers = FileRenamed;
            if (handlers != null)
                handlers(oldPath, newPath);
        }
        private void RootFileChangedMonitored(string path, bool monitored, FileChanged handlers)
        {
            if (!monitored)
                return;

            if (handlers != null)
                handlers(path);
        }

        private bool TryReachPath(string path, bool addNodes, out FileSystemNode destination)
        {
            destination = null;
            var pathParts = path.Split(Path.DirectorySeparatorChar);
            var currentNode = _root;
            foreach (var part in pathParts)
            {
                FileSystemNode child;
                if (!currentNode.TryGetChild(part, out child))
                {
                    if (!addNodes)
                        return false;
                    child = currentNode.GetOrAddChild(part);
                }
                currentNode = child;
            }

            destination = currentNode;
            return true;
        }

        private void BuildInnerFolders(string path, FileSystemNode parentNode)
        {
            var subdirectories = Directory.GetDirectories(path);
            foreach (var subdirectory in subdirectories)
            {
                var folderName = GetFolderName(subdirectory);
                var subdirectoryNode = parentNode.GetOrAddChild(folderName);
                BuildInnerFolders(subdirectory, subdirectoryNode);
            }
        }

        private string GetFolderName(string path)
        {
            var border = path.LastIndexOf(Path.DirectorySeparatorChar);
            var result = path.Substring(border + 1);
            return result;
        }
    }

    internal delegate void LocalFileChanged(string path, bool monitored);

    internal delegate void LocalFileRenamed(string oldPath, string newPath, bool monitored);
}