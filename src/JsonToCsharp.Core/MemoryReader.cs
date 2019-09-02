using System;

namespace JsonToCsharp.Core
{
    public class MemoryReader : ICharReader
    {
        private readonly string _text;

        public MemoryReader(string text)
        {
            _text = text;
        }

        public void Dispose() { }

        private int _offset;

        public int CurrentLine { get; private set; }
        public int CurrentLineOffset { get; private set; }

        public char ReadChar()
        {
            if (CanRead() == false) return '\0';

            var result = _text[_offset++];
            if (result == '\n')
            {
                CurrentLine += 1;
                CurrentLineOffset = 0;
            }

            CurrentLineOffset += 1;
            return result;
        }

        public char PeekChar() => CanRead() == false ? '\0' : _text[_offset];

        private bool CanRead() => _offset < _text.Length;
    }
}