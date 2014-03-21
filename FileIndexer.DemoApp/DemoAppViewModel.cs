using FileIndexer.Core.Interfaces;
using FileIndexer.DemoApp.Annotations;
using FileIndexer.FileSystemManagement;
using FileIndexer.Indexer;
using FileIndexer.Logging;
using FileIndexer.Quering;
using FileIndexer.TextProcessing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;

namespace FileIndexer.DemoApp
{
    internal sealed class DemoAppViewModel : INotifyPropertyChanged
    {
        private const char QuerySeparator = ';';
        private readonly RelayCommand _addFileCommand;
        private readonly RelayCommand _addFolderCommand;
        private readonly RelayCommand _executeQueryCommand;
        private readonly IFileIndexManager _fileIndexManager;
        private readonly ObservableCollection<string> _files = new ObservableCollection<string>();
        private readonly ObservableCollection<string> _folders = new ObservableCollection<string>();
        private readonly IWin32Window _oldStyleOwner;
        private ObservableCollection<string> _queryResult;

        private string _queryString;

        public DemoAppViewModel([NotNull] Window owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");

            QueryString = "word1;word2";
            _oldStyleOwner = new OldWindow(owner);
            _fileIndexManager = InitializeIndexManager();
            _addFolderCommand = new RelayCommand(obj => AddFolder());
            _addFileCommand = new RelayCommand(obj => AddFile());
            _executeQueryCommand = new RelayCommand(obj => ExecuteQuery());
        }

        public RelayCommand AddFolderCommand
        {
            get { return _addFolderCommand; }
        }

        public ObservableCollection<string> Folders
        {
            get { return _folders; }
        }

        public ObservableCollection<string> Files
        {
            get { return _files; }
        }

        public string QueryString
        {
            get { return _queryString; }
            set
            {
                _queryString = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand AddFileCommand
        {
            get { return _addFileCommand; }
        }

        public RelayCommand ExecuteQueryCommand
        {
            get { return _executeQueryCommand; }
        }

        public ObservableCollection<string> QueryResult
        {
            get { return _queryResult; }
            private set
            {
                _queryResult = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void ExecuteQuery()
        {
            if (string.IsNullOrWhiteSpace(_queryString))
                return;

            IEnumerable<string> queryParts = _queryString.ToLower()
                                                         .Split(QuerySeparator)
                                                         .Select(qp => qp.Trim());
            IIndexQuery[] queries = queryParts.Select(qp => new HasIndexQuery(qp))
                                              .Cast<IIndexQuery>()
                                              .ToArray();
            var executableQuery = new AndIndexQuery(queries);
            IEnumerable<string> executeResult = _fileIndexManager.QueryIndex(executableQuery)
                                                                 .Select(er => er.FullName);
            QueryResult = new ObservableCollection<string>(executeResult);
        }

        private IFileIndexManager InitializeIndexManager()
        {
            // poor man's DI
            var fileManager = new FileTree();
            var logger = new SillyDebugLogger();
            var tokenizer = new SimpleRegexTextTokenizer();
            var loader = new TextLoader();
            var fileIndexFactory = new FileIndexFactory(tokenizer, loader, logger);
            var result = new FileIndexManager(fileManager, logger, fileIndexFactory);
            return result;
        }

        private void AddFile()
        {
            var dialog = new OpenFileDialog {CheckFileExists = true, Filter = "(*.txt)|*.txt", Multiselect = true};
            DialogResult result = dialog.ShowDialog(_oldStyleOwner);
            if (result == DialogResult.OK)
            {
                foreach (string fileName in dialog.FileNames)
                {
                    _files.Add(fileName);
                }
                _fileIndexManager.AddFiles(dialog.FileNames);
            }
        }

        private void AddFolder()
        {
            var dialog = new FolderBrowserDialog {ShowNewFolderButton = false};
            DialogResult dialogResult = dialog.ShowDialog(_oldStyleOwner);
            if (dialogResult == DialogResult.OK)
            {
                _folders.Add(dialog.SelectedPath);
                _fileIndexManager.AddDirectories(new[] {dialog.SelectedPath});
            }
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}