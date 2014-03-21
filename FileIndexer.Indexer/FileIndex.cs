using FileIndexer.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace FileIndexer.Indexer
{
    public sealed class FileIndex : IFileIndex
    {
        private readonly IFileManager _fileManager;
        private readonly ITextTokenizer _tokenizer;
        private readonly ITextLoader _loader;
        private readonly ILogger _logger;

        private readonly Dictionary<IWord, HashSet<string>> _wordIndex = new Dictionary<IWord, HashSet<string>>();  
        private readonly ReadOnlyDictionary<IWord, HashSet<string>> _readOnlyIndex;
        private readonly Dictionary<string, HashSet<IWord>> _filesWord = new Dictionary<string, HashSet<IWord>>(); 
        private readonly Dictionary<string, DateTime> _lastFilesUpdates = new Dictionary<string, DateTime>(); // TODO: remove entries for deleted files periodically

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public FileIndex(IFileManager fileManager, ITextTokenizer tokenizer, ITextLoader loader, ILogger logger)
        {
            if (fileManager == null) throw new ArgumentNullException("fileManager");
            if (tokenizer == null) throw new ArgumentNullException("tokenizer");
            if (loader == null) throw new ArgumentNullException("loader");
            if (logger == null) throw new ArgumentNullException("logger");

            _fileManager = fileManager;
            _tokenizer = tokenizer;
            _loader = loader;
            _logger = logger;
            _readOnlyIndex = new ReadOnlyDictionary<IWord, HashSet<string>>(_wordIndex);

            HookEventProcessing();
        }

        private void HookEventProcessing()
        {
            _fileManager.FileAdded += FileManagerOnFileAdded;
            _fileManager.FileChanged += FileManagerOnFileChanged;
            _fileManager.FileRemoved += FileManagerOnFileRemoved;
            _fileManager.FileRenamed += FileManagerOnFileRenamed;
        }

        private void FileManagerOnFileRenamed(string oldPath, string newPath)
        {
            if (string.IsNullOrWhiteSpace(oldPath)) throw new ArgumentException();
            if (string.IsNullOrWhiteSpace(newPath)) throw new ArgumentException();

            var timeStamp = DateTime.Now;
            _lock.EnterWriteLock();
            RenameIndexedFile(oldPath, newPath, timeStamp);
            _lock.ExitWriteLock();
        }

        private void FileManagerOnFileRemoved(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException();

            var timeStamp = DateTime.Now;
            _lock.EnterWriteLock();
            RemoveFileIndex(filePath, timeStamp);
            _lock.ExitWriteLock();
        }

        private void FileManagerOnFileChanged(string filePath)
        {
            AddFileContentToIndex(filePath);
        }

        private void FileManagerOnFileAdded(string filePath)
        {
            AddFileContentToIndex(filePath);
        }

        private void AddFileContentToIndex(string filePath)
        {
            var timeStamp = DateTime.Now;

            IEnumerable<IWord> wordStream;
            if (!TryLoadTokenSequence(filePath, out wordStream)) return;

            _lock.EnterWriteLock();
            ProcessTokenStream(wordStream, filePath, timeStamp);
            _lock.ExitWriteLock();
        }

        private bool TryLoadTokenSequence(string filePath, out IEnumerable<IWord> wordStream)
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException();

            wordStream = null;
            string text;
            if (!TryLoadText(filePath, out text)) return false;

            wordStream = _tokenizer.Tokenize(text);
            return true;
        }

        private bool TryLoadText(string filePath, out string text)
        {
            text = null;
            try
            {
                text = _loader.LoadText(filePath);
            }
            catch (Exception ex)
            {
                _logger.Log(ex.Message);
                return false;
            }
            return true;
        }

        private void RenameIndexedFile(string oldName, string newName, DateTime timeStamp)
        {
            var normalizedOldName = oldName.ToLower();
            var normalizedNewName = newName.ToLower();

            if(!_filesWord.ContainsKey(normalizedOldName))
                return;

            if (_lastFilesUpdates.ContainsKey(normalizedNewName) && _lastFilesUpdates[normalizedNewName] > timeStamp)
            {
                if(_lastFilesUpdates[normalizedOldName] < timeStamp)
                    RemoveFileIndex(normalizedOldName, timeStamp);
                return;
            }

            var words = _filesWord[normalizedOldName];
            foreach (var word in words)
            {
                var wordFiles = _wordIndex[word];
                wordFiles.Remove(normalizedOldName);
                wordFiles.Add(normalizedNewName);
            }

            _filesWord.Remove(normalizedOldName);
            _filesWord.Add(normalizedNewName, words);
            _lastFilesUpdates[normalizedOldName] = timeStamp;
            _lastFilesUpdates[normalizedNewName] = timeStamp;
        }

        private void RemoveFileIndex(string filePath, DateTime timeStamp)
        {
            var normalizePath = filePath.ToLower();

            HashSet<IWord> words;
            if(!_filesWord.TryGetValue(normalizePath, out words) || _lastFilesUpdates[normalizePath] > timeStamp)
                return;

            foreach (var word in words)
            {
                var filesWithWord = _wordIndex[word];
                filesWithWord.Remove(normalizePath);
                if (!filesWithWord.Any())
                    _wordIndex.Remove(word);
            }
            _filesWord.Remove(normalizePath);

            _lastFilesUpdates[normalizePath] = timeStamp;
        }

        private void ProcessTokenStream(IEnumerable<IWord> tokens, string filePath, DateTime timeStamp)
        {
            var normalizedPath = filePath.ToLower();

            if(_lastFilesUpdates.ContainsKey(normalizedPath) && _lastFilesUpdates[normalizedPath] > timeStamp)
                return;

            if(_filesWord.ContainsKey(filePath))
                RemoveFileIndex(filePath, timeStamp);

            var fileWords = new HashSet<IWord>();
            _filesWord.Add(normalizedPath, fileWords);

            foreach (var token in tokens.Distinct())
            {
                if(!_wordIndex.ContainsKey(token))
                    _wordIndex.Add(token, new HashSet<string>());

                _wordIndex[token].Add(filePath);
                fileWords.Add(token);
            }

            _lastFilesUpdates[normalizedPath] = timeStamp;
        }

        public IEnumerable<string> ExecuteQuery(IIndexQuery query)
        {
            if (query == null) throw new ArgumentNullException("query");

            _lock.EnterReadLock();
            var result = query.Execute(_readOnlyIndex);
            _lock.ExitReadLock();

            return result;
        }
    }
}