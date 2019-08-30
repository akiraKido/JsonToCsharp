using System;
using System.Collections.Generic;
using System.Text;

namespace JsonToCsharp
{
    internal enum TokenType
    {
        None,
        EndOfFile,

        L_Brace,
        R_Brace,
        L_Bracket,
        R_Bracket,
        Colon,
        String,
        Number,
        Comma,

        Boolean,
    }

    internal class Lexer : IDisposable
    {
        private readonly ICharReader _reader;
        private (TokenType tokenType, string value) _token = (TokenType.None, "\0");

        internal (TokenType tokenType, string value) Token
        {
            get
            {
                if (_token.tokenType == TokenType.None)
                {
                    Advance();
                }

                return _token;
            }
        }

        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private static readonly (TokenType tokenType, string value) EndOfFile = (TokenType.EndOfFile, "\0");

        private static readonly Dictionary<char, (TokenType tokenType, string value)> SingleToken
            = new Dictionary<char, (TokenType tokenType, string value)>
            {
                {'{', (TokenType.L_Brace, "{")},
                {'}', (TokenType.R_Brace, "}")},
                {':', (TokenType.Colon, ":")},
                {',', (TokenType.Comma, ",")},
                {'[', (TokenType.L_Bracket, "[")},
                {']', (TokenType.R_Bracket, "]")},
            };

        private static readonly Dictionary<string, (TokenType tokenType, string value)> PredefinedIdentifiers
            = new Dictionary<string, (TokenType tokenType, string value)>
            {
                {"true", (TokenType.Boolean, "true")},
                {"false", (TokenType.Boolean, "false")},
            };

        internal Lexer(ICharReader reader)
        {
            _reader = reader;
        }

        public void CheckAndAdvance(TokenType tokenType)
        {
            if (Token.tokenType != tokenType)
            {
                throw Error.UnexpectedToken(_reader, ":", Token.value);
            }

            Advance();
        }

        internal void Advance()
        {
            var c = _reader.ReadChar();
            while (char.IsWhiteSpace(c))
            {
                c = _reader.ReadChar();
            }

            if (c == '\0')
            {
                _token = EndOfFile;
                return;
            }

            if (SingleToken.ContainsKey(c))
            {
                _token = SingleToken[c];
                return;
            }

            if (c == '"')
            {
                char prev;
                c = '\0';
                while (true)
                {
                    prev = c;
                    if ((c = _reader.ReadChar()) == '"')
                    {
                        if (prev != '\\' && prev != '\0')
                        {
                            break;
                        }
                    }

                    if (c == '\0')
                    {
                        throw new Exception("File ended in middle of string.");
                    }

                    _stringBuilder.Append(c);
                }

                _token = (TokenType.String, _stringBuilder.ToString());
                _stringBuilder.Clear();
                return;
            }

            if (char.IsDigit(c))
            {
                _stringBuilder.Append(c);

                var peek = _reader.PeekChar();
                
                while (char.IsDigit(peek = _reader.PeekChar()) || peek == '.')
                {
                    _stringBuilder.Append(_reader.ReadChar());
                }

                _token = (TokenType.Number, _stringBuilder.ToString());
                _stringBuilder.Clear();
                return;
            }

            if (char.IsLetter(c))
            {
                while (char.IsLetter(c))
                {
                    _stringBuilder.Append(c);
                    c = _reader.ReadChar();
                }

                var result = _stringBuilder.ToString();
                _stringBuilder.Clear();

                if (PredefinedIdentifiers.ContainsKey(result))
                {
                    _token = PredefinedIdentifiers[result];
                    return;
                }
            }

            throw new Exception($"Unexpected char {c}");
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}