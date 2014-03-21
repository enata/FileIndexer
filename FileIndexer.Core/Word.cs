using FileIndexer.Core.Interfaces;
using System;

namespace FileIndexer.Core
{
    public sealed class Word : IWord
    {
        private readonly string _text;

        public Word(string text)
        {
            if (text == null) throw new ArgumentNullException("text");

            _text = text;
        }

        public string Text
        {
            get { return _text; }
        }

        private bool Equals(Word other)
        {
            return string.Equals(_text, other._text);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Word && Equals((Word) obj);
        }

        public override int GetHashCode()
        {
            return _text.GetHashCode();
        }
    }
}