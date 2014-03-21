using System.Linq;
using FileIndexer.Core.Interfaces;
using FileIndexer.TextProcessing;
using NUnit.Framework;

namespace FileIndexer.Tets.TextProcessing
{
    [TestFixture]
    public sealed class SimpleRegexTextTokenizerTests
    {
        [Test]
        public void TokenizeTest()
        {
            const string text = "Белеет парус одинокый...";
            var tokenizer = new SimpleRegexTextTokenizer();

            IWord[] tokens = tokenizer.Tokenize(text)
                                      .ToArray();

            Assert.AreEqual(3, tokens.Length);
            Assert.AreEqual("белеет", tokens[0].Text);
            Assert.AreEqual("парус", tokens[1].Text);
            Assert.AreEqual("одинокый", tokens[2].Text);
        }
    }
}