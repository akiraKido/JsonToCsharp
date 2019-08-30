using System;
using System.IO;

namespace JsonToCsharp.Core
{
    public interface ICharReader : IDisposable
    {
        int CurrentLine { get; }
        int CurrentLineOffset { get; }
        char ReadChar();
        char PeekChar();
    }

    public class FileReader : ICharReader
    {
        private readonly char[] _buffer = new char[1024];
        private int _bufferLength = 0;
        private readonly StreamReader _reader;
        private int _offset = 0;

        public int CurrentLine { get; private set; }
        public int CurrentLineOffset { get; private set; }

        public FileReader(string path)
        {
            _reader = File.OpenText(path);
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        public char PeekChar()
        {
            return CanRead() == false 
                ? '\0' 
                : _buffer[_offset];
        }

        public char ReadChar()
        {
            if (CanRead() == false)
            {
                return '\0';
            }

            var result = _buffer[_offset++];
            if (result == '\n')
            {
                CurrentLine++;
                CurrentLineOffset = 0;
            }

            CurrentLineOffset++;
            return result;
        }

        private bool CanRead()
        {
            if (_offset >= _bufferLength)
            {
                _bufferLength = _reader.ReadBlock(_buffer, 0, _buffer.Length);
                if (_bufferLength <= 0)
                {
                    return false;
                }

                _offset = 0;
            }

            return true;
        }
    }
}