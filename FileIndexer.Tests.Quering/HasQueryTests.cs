using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileIndexer.Core;
using FileIndexer.Core.Interfaces;
using FileIndexer.Quering;
using NUnit.Framework;
using Rhino.Mocks;

namespace FileIndexer.Tests.Quering
{
    [TestFixture]
    public sealed class HasIndexQueryTests
    {
        [Test]
        public void ExecuteTest()
        {
            var index = new Dictionary<IWord, HashSet<string>>();
            var dictionaryWrapper = new ReadOnlyDictionary<IWord, HashSet<string>>(index);
            var word1 = new Word("w1");
            var word2 = new Word("w2");
            var fileSet1 = new HashSet<string> {"f1"};
            var fileSet2 = new HashSet<string> {"f2"};
            index.Add(word1, fileSet1);
            index.Add(word2, fileSet2);
            var query = new HasIndexQuery("w1");

            var result = query.Execute(dictionaryWrapper).ToArray();

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("f1", result.Single());
        }
    }

    [TestFixture]
    public sealed class AndIndexQueryTest
    {
        [Test]
        public void ExecuteTest()
        {
            var index = new Dictionary<IWord, HashSet<string>>();
            var dictionaryWrapper = new ReadOnlyDictionary<IWord, HashSet<string>>(index);
            var word1 = new Word("w1");
            var word2 = new Word("w2");
            var fileSet1 = new HashSet<string> { "f1", "f2" };
            var fileSet2 = new HashSet<string> { "f2" };
            index.Add(word1, fileSet1);
            index.Add(word2, fileSet2);
            var query = new AndIndexQuery(new HasIndexQuery("w1"), new HasIndexQuery("w2"));

            var result = query.Execute(dictionaryWrapper).ToArray();

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("f2", result.Single());
        }
    }

    [TestFixture]
    public sealed class OrIndexQueryTest
    {
        [Test]
        public void ExecuteTest()
        {
            var index = new Dictionary<IWord, HashSet<string>>();
            var dictionaryWrapper = new ReadOnlyDictionary<IWord, HashSet<string>>(index);
            var word1 = new Word("w1");
            var word2 = new Word("w2");
            var word3 = new Word("w3");
            var fileSet1 = new HashSet<string> { "f1", "f2" };
            var fileSet2 = new HashSet<string> { "f2" };
            var fileSet3 = new HashSet<string> {"f3"};
            index.Add(word1, fileSet1);
            index.Add(word2, fileSet2);
            index.Add(word3, fileSet3);
            var query = new OrIndexQuery(new HasIndexQuery("w1"), new HasIndexQuery("w2"));

            var result = query.Execute(dictionaryWrapper).ToArray();

            Assert.AreEqual(2, result.Length);
            Assert.IsTrue(result.Contains("f1"));
            Assert.IsTrue(result.Contains("f2"));
        }
    }
}
