using System.Collections.Generic;
using System.Linq;

namespace FileIndexer.FileSystemManagement
{
    internal sealed class FileFilter
    {
        private HashSet<string> _filesOfInterest = new HashSet<string>();
        private bool _enabled;

        public void AddFile(string fileName)
        {
            _filesOfInterest.Add(fileName);
        }

        public bool RemoveFile(string fileName)
        {
            return _filesOfInterest.Remove(fileName);
        }

        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        public bool Pass(string fileName)
        {
            return !_enabled || _filesOfInterest.Contains(fileName);
        }

        public void UpdateFileNames(string oldPath, string newPath)
        {
            _filesOfInterest = new HashSet<string>(_filesOfInterest.Select(foi => foi.Replace(oldPath, newPath)));
        }
    }
}