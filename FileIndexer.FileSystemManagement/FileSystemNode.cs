using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FileIndexer.FileSystemManagement
{
    // To monitor removing and renaming of files parent folders we should mantain the whole file system tree
    // or scan the directories periodically. Either way sucks = (

    internal sealed class FileSystemNode
    {
        private string _name;
        private readonly Dictionary<string, FileSystemNode> _next = new Dictionary<string, FileSystemNode>();
        private string _parentLocation; // caches parent folder path

        private readonly DirectoryObservation _observation;
        private readonly DirectoryObservationFactory _directoryObservationFactory;

        private FileSystemNode _parent;

        private readonly object _syncRoot = new object(); 

        private readonly FileFilter _filter;


        public FileSystemNode(string name, bool monitorFolder) : this(name, null, monitorFolder, new DirectoryObservationFactory())
        {}

        public FileSystemNode(string name, FileSystemNode parent, bool monitorFolder, DirectoryObservationFactory directoryObservationFactory)
        {
            _directoryObservationFactory = directoryObservationFactory;
            _name = name.ToLower();
            if(parent != null)
                parent.AddChild(this);
            _filter = new FileFilter {Enabled = !monitorFolder};

            _parentLocation = BuildParentLocation();
            var fullPath = BuildFullPath();

            if (!string.IsNullOrEmpty(fullPath))
            {
                _observation = _directoryObservationFactory.Produce(fullPath);
                BindFileObservations();
                _observation.ForceAdddEvent();
            }
        }

        public event LocalFileChanged FileChanged;
        public event LocalFileChanged FileAdded;
        public event LocalFileChanged FileRemoved;
        public event LocalFileRenamed FileRenamed;

        public FileSystemNode GetOrAddChild(string name)
        {
            lock (_syncRoot)
            {
                FileSystemNode result;
                if (_next.TryGetValue(name, out result))
                    return result;
                result = new FileSystemNode(name, this, false, _directoryObservationFactory);
                return result;
            }
        }

        public bool TryGetChild(string name, out FileSystemNode child)
        {
            lock (_syncRoot)
            {
                return _next.TryGetValue(name, out child);
            }
        }

        public void MonitorFile(string filePath)
        {
            var normalizedFilePath = filePath.ToLower();
            lock (_syncRoot)
            {
                _filter.AddFile(normalizedFilePath);
                OnFileChangedRooter(normalizedFilePath, FileAdded);
            }
        }

        public bool StopMonitoringFile(string filePath)
        {
            var normalizedFilePath = filePath.ToLower();
            lock (_syncRoot)
            {
                if (_filter.RemoveFile(normalizedFilePath))
                {
                    OnFileChangedRooter(normalizedFilePath, FileRemoved);
                    return true;
                }

            }
            return false;
        }

        private string BuildParentLocation()
        {
            var result = _parent == null ? string.Empty : _parent.BuildFullPath();
            return result;
        }

        private void BindFileObservations()
        {
            _observation.FileChanged += ObservationOnFileChanged;
            _observation.FileAdded += ObservationOnFileAdded;
            _observation.FileRemoved += ObservationOnFileRemoved;
            _observation.FileRenamed += ObservationOnFileRenamed;
            _observation.DirectoryRemoved += ObservationOnDirectoryRemoved;
            _observation.DirectoryRenamed += ObservationOnDirectoryRenamed;
        }

        private void ObservationOnDirectoryRenamed(string oldPath, string newPath)
        {
            lock (_syncRoot)
            {
                if(oldPath == newPath)
                    return;
                FileSystemNode renamed;
                if (_next.TryGetValue(oldPath, out renamed) && !_next.ContainsKey(newPath))
                {
                    renamed._name = newPath;
                    var oldName = _parentLocation + Path.DirectorySeparatorChar + _name + Path.DirectorySeparatorChar +
                                  oldPath;
                    var newName = _parentLocation + Path.DirectorySeparatorChar + _name + Path.DirectorySeparatorChar +
                                  newPath;
                    renamed.PropagateName(oldName, newName);
                    _next.Remove(oldPath);
                    _next.Add(newPath, renamed);
                    
                }
            }
        }

        private void PropagateName(string oldPath, string newPath)
        {
            
            FileSystemNode[] children;     
            lock (_syncRoot)
            {
                _parentLocation = BuildParentLocation();
                RenameFiles(oldPath, newPath);
                _filter.UpdateFileNames(oldPath, newPath);
                children = _next.Values.ToArray();
            }

            foreach (var node in children)
            {
                node.PropagateName(oldPath, newPath);
            }
        }

        private void ObservationOnDirectoryRemoved(string directory)
        {
            lock (_syncRoot)
            {
                FileSystemNode removed;
                if (_next.TryGetValue(directory, out removed))
                {
                    removed.RemoveAllFiles();
                    _next.Remove(directory);
                    UnsubscribeForChild(removed);
                }
            }
        }

        private void RenameFiles(string oldName, string newName)
        {
            _observation.ForceRenameEvent(oldName, newName);
        }

        private void RemoveAllFiles()
        {
            _observation.ForceRemoveEvent();
            foreach (var node in _next.Values)
            {
                node.RemoveAllFiles();
            }
        }

        private void OnFileChangedRooter(string filePath, LocalFileChanged target)
        {
            if (target != null)
                target(filePath, _filter.Pass(filePath));
        }

        private void ObservationOnFileRenamed(string oldPath, string newPath)
        {
            var renameHandlers = FileRenamed;
            if (renameHandlers != null)
                renameHandlers(oldPath, newPath, _filter.Pass(oldPath));
        }

        private void ObservationOnFileRemoved(string filePath) 
        {
            OnFileChangedRooter(filePath, FileRemoved);
        }

        private void ObservationOnFileAdded(string filePath)
        {
            OnFileChangedRooter(filePath, FileAdded);
        }

        private void ObservationOnFileChanged(string filePath)
        {
            OnFileChangedRooter(filePath, FileChanged);
        }


        private void UnsubscribeForChild(FileSystemNode child)
        {
            child.FileAdded -= ChildOnFileAdded;
            child.FileChanged -= ChildOnFileChanged;
            child.FileRemoved -= ChildOnFileRemoved;
            child.FileRenamed -= ChildOnFileRenamed;
        }

        private void AddChild(FileSystemNode child)
        {
            lock(_syncRoot)
            {
                if (!_next.ContainsKey(child.Name))
                {
                    _next.Add(child.Name, child);
                    InitChild(child);
                }
            }
        }

        private void InitChild(FileSystemNode child)
        {
            child.Parent = this;         
            child.FileAdded += ChildOnFileAdded;
            child.FileChanged += ChildOnFileChanged;
            child.FileRemoved += ChildOnFileRemoved;
            child.FileRenamed += ChildOnFileRenamed;
        }

        private void ChildOnFileRenamed(string oldPath, string newPath, bool monitored)
        {
            var renameHandler = FileRenamed;
            if (renameHandler != null)
                renameHandler(oldPath, newPath, monitored || MonitorFolder);
        }

        private void ChildOnFileRemoved(string path, bool monitored)
        {
            var removeHandlers = FileRemoved;
            if (removeHandlers != null)
                removeHandlers(path, monitored || MonitorFolder);
        }

        private void ChildOnFileChanged(string path, bool monitored)
        {
            var chandeHandlers = FileChanged;
            if (chandeHandlers != null)
                chandeHandlers(path, monitored || MonitorFolder);
        }

        private void ChildOnFileAdded(string path, bool monitored)
        {
            var addHandlers = FileAdded;
            if (addHandlers != null)
                addHandlers(path, monitored || MonitorFolder);
        }

        public bool MonitorFolder
        {
            private get { return !_filter.Enabled; }
            set
            {
                if (_filter.Enabled && value)
                {
                    _filter.Enabled = false;
                    _observation.ForceAdddEvent();
                }
                else if (!_filter.Enabled && !value)
                {
                    _observation.ForceRemoveEvent();
                    _filter.Enabled = true;
                }
            }
        }


        private string BuildFullPath()
        {
            var result = string.IsNullOrEmpty(_parentLocation) ? _name : _parentLocation + Path.DirectorySeparatorChar + _name;
            return result;
        }


        private string Name
        {
            get { return _name; }
        }

        private FileSystemNode Parent
        {
            set
            {
                _parent = value;
                _parentLocation = _parent.BuildFullPath();
            }
        }

    }
}