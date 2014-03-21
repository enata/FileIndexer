using System;
using System.Collections.Generic;
using System.Linq;
using FileIndexer.Core.Interfaces;
using FileIndexer.Indexer;
using NUnit.Framework;
using Rhino.Mocks;

namespace FileIndexer.Tests.Indexer
{
    [TestFixture]
    public sealed class FileIndexTests
    {
        [Test]
        public void ExecuteQueryTest()
        {
            var queryResult = new[] {"str"};
            var query = MockRepository.GenerateStub<IIndexQuery>();
            query.Stub(q => q.Execute(Arg<IReadOnlyDictionary<IWord, HashSet<string>>>.Is.Anything))
                 .Return(queryResult);
            var fileManager = MockRepository.GenerateStub<IFileManager>();
            var tokenizer = MockRepository.GenerateStub<ITextTokenizer>();
            var loader = MockRepository.GenerateStub<ITextLoader>();
            var logger = MockRepository.GenerateStub<ILogger>();
            var fileIndex = new FileIndex(fileManager, tokenizer, loader, logger);

            var result = fileIndex.ExecuteQuery(query).ToArray();

            Assert.AreEqual(queryResult.Length, result.Length);
            Assert.AreEqual(queryResult[0], result[0]);
        }

        [Test]
        public void FileAddedToIndexTest()
        {
            IReadOnlyDictionary<IWord, HashSet<string>> rawIndex;
            InitializeIndex(out rawIndex, manager => { });

            Assert.AreEqual(1, rawIndex.Count);
            var word = rawIndex.First()
                               .Key;
            Assert.AreEqual("f1w1", word.Text);
            Assert.IsTrue(rawIndex[word].Contains("f1"));
        }

        [Test]
        public void FileRemovedFromIndexTest()
        {
            IReadOnlyDictionary<IWord, HashSet<string>> rawIndex;
            InitializeIndex(out rawIndex, manager => manager.FireFileRemoved("f1"));

            Assert.IsEmpty(rawIndex);
        }

        [Test]
        public void FileChangedTest()
        {
            IReadOnlyDictionary<IWord, HashSet<string>> rawIndex;
            InitializeIndex(out rawIndex, manager => manager.FireFileChanged("f1"));

            Assert.AreEqual(1, rawIndex.Count);
            var word = rawIndex.First()
                               .Key;
            Assert.AreEqual("f1w2", word.Text);
            Assert.IsTrue(rawIndex[word].Contains("f1"));
        }

        [Test]
        public void FileRenamedTest()
        {
            IReadOnlyDictionary<IWord, HashSet<string>> rawIndex;
            InitializeIndex(out rawIndex, manager => manager.FireFileRenamed("f1", "f2"));

            Assert.AreEqual(1, rawIndex.Count);
            var word = rawIndex.First()
                               .Key;
            Assert.AreEqual("f1w1", word.Text);
            Assert.IsTrue(rawIndex[word].Contains("f2"));
            Assert.IsFalse(rawIndex[word].Contains("f1"));
        }

        private void InitializeIndex(out IReadOnlyDictionary<IWord, HashSet<string>> rawIndex, Action<FileManagerStub> fireEvents)
        {
            IReadOnlyDictionary<IWord, HashSet<string>> index = null;
            var fileManager = new FileManagerStub();
            var tokenizer = MockRepository.GenerateStub<ITextTokenizer>();

            tokenizer.Stub(t => t.Tokenize(Arg<string>.Is.Anything))
                     .Do(new Func<string, IEnumerable<IWord>>(s =>
                         {
                             var w = MockRepository.GenerateStub<IWord>();
                             w.Stub(wrd => wrd.Text)
                              .Return(s);
                             return new[] {w};
                         }));
            var loader = MockRepository.GenerateStub<ITextLoader>();
            int i = 1;
            loader.Stub(l => l.LoadText(Arg<string>.Is.Anything))
                  .Do(new Func<string, string>(s => s + "w" + i++));
            var logger = MockRepository.GenerateStub<ILogger>();
            var query = MockRepository.GenerateStub<IIndexQuery>();
            query.Stub(q => q.Execute(Arg<IReadOnlyDictionary<IWord, HashSet<string>>>.Is.Anything))
                 .Do(new Func<IReadOnlyDictionary<IWord, HashSet<string>>, IEnumerable<string>>(pairs =>
                     {
                         index = pairs;
                         return Enumerable.Empty<string>();
                     }));
            var fileIndex = new FileIndex(fileManager, tokenizer, loader, logger);

            fileManager.FireFileAdded("f1");
            fireEvents(fileManager);
            fileIndex.ExecuteQuery(query);
            rawIndex = index;

        }
    }
}