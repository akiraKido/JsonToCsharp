using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonToCsharp.Core
{
    using static Console;

    public class JsonToCsharpGenerator
    {
        private readonly IReadOnlyOptions _options;

        private readonly Dictionary<string, string> _classResults = new Dictionary<string, string>();

        public JsonToCsharpGenerator() => _options = new Options();

        public JsonToCsharpGenerator(IReadOnlyOptions options) => _options = options;

        public IReadOnlyDictionary<string, string> Create(string name, ICharReader reader)
        {
            using (var lexer = new Lexer(reader))
            {
                CreateEntry(name, reader, lexer);
            }

            return _classResults;
        }

        private static readonly HashSet<string> PredefinedCsharpIdentifiers =
            new HashSet<string>(new[]
            {
                "abstract",
                "as",
                "base",
                "bool",
                "break",
                "byte",
                "case",
                "catch",
                "char",
                "checked",
                "class",
                "const",
                "continue",
                "decimal",
                "default",
                "delegate",
                "do",
                "double",
                "else",
                "enum",
                "event",
                "explicit",
                "extern",
                "false",
                "finally",
                "fixed",
                "float",
                "for",
                "foreach",
                "goto",
                "if",
                "implicit",
                "in",
                "int",
                "interface",
                "internal",
                "is",
                "lock",
                "long",
                "namespace",
                "new",
                "null",
                "object",
                "operator",
                "out",
                "override",
                "params",
                "private",
                "protected",
                "public",
                "readonly",
                "ref",
                "return",
                "sbyte",
                "sealed",
                "short",
                "sizeof",
                "stackalloc",
                "static",
                "string",
                "struct",
                "switch",
                "this",
                "throw",
                "true",
                "try",
                "typeof",
                "uint",
                "ulong",
                "unchecked",
                "unsafe",
                "ushort",
                "using",
                "using static",
                "virtual",
                "void",
                "volatile",
                "while",
            });

        private string CreateEntryAsDictionary(string name, ICharReader reader, Lexer lexer)
        {
            // Create normal dictionary instead of class = skip a layer
            lexer.Advance(); // eat number
            lexer.CheckAndAdvance(TokenType.Colon); // eat colon
            string childEntry;
            if (lexer.Token.tokenType == TokenType.L_Brace)
            {
                childEntry = CreateEntry(name, reader, lexer);
            }
            else
            {
                childEntry = GetStandardType(reader, lexer.Token);
                lexer.Advance(); // eat standard
            }

            void SkipBlock()
            {
                while (lexer.Token.tokenType != TokenType.R_Brace
                       && lexer.Token.tokenType != TokenType.Comma)
                {
                    lexer.Advance();

                    if (lexer.Token.tokenType == TokenType.L_Brace)
                    {
                        SkipBlock();
                        lexer.CheckAndAdvance(TokenType.R_Brace);
                    }
                }
            }

            while (lexer.Token.tokenType == TokenType.Comma)
            {
                lexer.CheckAndAdvance(TokenType.Comma);
                SkipBlock();
            }

            lexer.CheckAndAdvance(TokenType.R_Brace);

            return $"IReadOnlyDictionary<int, {childEntry}>";
        }

        private string CreateEntryAsClass(string name, ICharReader reader, Lexer lexer)
        {
            var items = new List<(string, string)>();

            while (lexer.Token.tokenType != TokenType.R_Brace)
            {
                var item = GetItem(reader, lexer);
                items.Add(item);
            }

            lexer.Advance();

            var className = name.SnakeToUpperCamel();
            var result = CreateImmutableClass(name, items);
            _classResults[className] = result;

            return name.SnakeToUpperCamel();
        }

        private string CreateEntry(string name, ICharReader reader, Lexer lexer)
        {
            var token = lexer.Token;

            if (token.tokenType != TokenType.L_Brace)
            {
                WriteLine(token.value);
                throw CreateException(reader, "expected {");
            }

            lexer.Advance(); // eat {

            return int.TryParse(lexer.Token.value, out _)
                ? CreateEntryAsDictionary(name, reader, lexer)
                : CreateEntryAsClass(name, reader, lexer);
        }

        private string CreateImmutableClass(string name, IReadOnlyList<(string name, string type)> items)
        {
            var nameCamelUpper = name.SnakeToUpperCamel();

            var builder = new ClassBuilder();

            builder += "using System;";

            {
                // Search for any list types
                // TODO: should be a better way...
                var listTypeEnumNames = Enum.GetNames(typeof(ListType));

                foreach (var item in items)
                {
                    if (listTypeEnumNames.Any(listType => item.type.Contains(listType)))
                    {
                        builder += "using System.Collections.Generic;";
                        goto DONE;
                    }
                }
            }
            DONE:

            if (_options.DeclareDataMember)
            {
                builder += "using System.Runtime.Serialization;";
            }

            builder.AddLine();

            if (string.IsNullOrWhiteSpace(_options.NameSpace) == false)
            {
                builder += $"namespace {_options.NameSpace}";
                builder += "{";
                builder.Indent();
            }

            builder += $"public class {nameCamelUpper}";
            builder += "{";
            builder.Indent();
            builder += $"public {nameCamelUpper}";
            builder += "(";
            builder.Indent();
            foreach (var (s, type) in items.Take(items.Count - 1))
            {
                var itemName = s;
                builder += PredefinedCsharpIdentifiers.Contains(itemName)
                    ? $"{type} {itemName.SnakeToUpperCamel()}"
                    : $"{type} {itemName},";
            }

            var lastParameter = items.Last();
            {
                var itemName = lastParameter.name;
                if (PredefinedCsharpIdentifiers.Contains(itemName))
                {
                    itemName = $"{itemName.SnakeToUpperCamel()}";
                }

                builder += $"{lastParameter.type} {itemName}";
            }
            builder.Dedent();
            builder += $")";
            builder += $"{{";
            builder.Indent();
            foreach (var item in items)
            {
                var itemName = item.name;
                if (PredefinedCsharpIdentifiers.Contains(itemName))
                {
                    itemName = $"{itemName.SnakeToUpperCamel()}";
                }

                builder += $"this.{item.name.SnakeToUpperCamel()} = {itemName};";
            }

            builder.Dedent();
            builder += $"}}";
            foreach (var item in items)
            {
                if (_options.DeclareDataMember)
                {
                    builder += $"[DataMember(Name = \"{item.name}\")]";
                }

                builder += $"public {item.type} {item.name.SnakeToUpperCamel()} {{ get; }}";
            }

            builder.Dedent();
            builder += $"}}";

            if (string.IsNullOrWhiteSpace(_options.NameSpace) == false)
            {
                builder.Dedent();
                builder += $"}}";
            }

            return builder.ToString();
        }

        private (string, string) GetItem(ICharReader reader, Lexer lexer)
        {
            var key = GetKey(reader, lexer);
            lexer.CheckAndAdvance(TokenType.Colon);

            string type;
            switch (lexer.Token.tokenType)
            {
                case TokenType.Number:
                case TokenType.String:
                case TokenType.Boolean:
                    type = GetStandardType(reader, lexer.Token);
                    lexer.Advance(); // eat token
                    break;
                case TokenType.L_Brace:
                    type = CreateEntry(key, reader, lexer);
                    break;
                case TokenType.L_Bracket:
                    var genericType = GetArrayGenericType(key, reader, lexer);
                    type = $"{_options.ListType.ToString()}<{genericType}>";
                    break;
                case TokenType.None:
                case TokenType.EndOfFile:
                case TokenType.R_Brace:
                case TokenType.R_Bracket:
                case TokenType.Colon:
                case TokenType.Comma:
                default:
                    throw CreateException(reader, "unexpected value - expected number or string");
            }

            if (lexer.Token.tokenType == TokenType.Comma)
            {
                lexer.Advance();
                if (lexer.Token.tokenType == TokenType.R_Brace)
                {
                    throw CreateException(reader, "trailing commas are not valid");
                }
            }

            return (key, type);
        }


        private static string GetKey(ICharReader reader, Lexer lexer)
        {
            if (lexer.Token.tokenType != TokenType.String)
            {
                throw CreateException(reader, "expected string");
            }

            var key = lexer.Token.value;
            lexer.Advance();
            return key;
        }

        private string GetArrayGenericType(string key, ICharReader reader, Lexer lexer)
        {
            lexer.Advance(); // eat [
            var token = lexer.Token;

            string genericType;
            if (token.tokenType == TokenType.L_Brace)
            {
                string arrayEntryName = key.SnakeToUpperCamel();
                if (arrayEntryName.EndsWith("s"))
                {
                    arrayEntryName = arrayEntryName.Substring(0, arrayEntryName.Length - 1);
                }
                else
                {
                    arrayEntryName = $"{key.SnakeToUpperCamel()}Entry";
                }

                genericType = arrayEntryName;
                CreateEntry(arrayEntryName, reader, lexer);
                int nest = 0;
                while (true)
                {
                    // ignore all elements other than first element
                    token = lexer.Token;
                    if (token.tokenType == TokenType.R_Bracket)
                    {
                        if (nest == 0)
                        {
                            break;
                        }

                        nest--;
                    }

                    if (token.tokenType == TokenType.L_Bracket)
                    {
                        nest++;
                    }

                    lexer.Advance();
                }
            }
            else
            {
                genericType = GetStandardType(reader, token);
                lexer.Advance();
                token = lexer.Token;
                while (token.tokenType != TokenType.R_Bracket)
                {
                    lexer.Advance();
                    token = lexer.Token;
                }
            }

            lexer.Advance(); // eat ]
            return genericType;
        }

        private static string GetStandardType(ICharReader reader, (TokenType tokenType, string value) token)
        {
            switch (token.tokenType)
            {
                case TokenType.String:
                    if (DateTime.TryParse(token.value, out _))
                    {
                        return "DateTime";
                    }

                    return "string";
                case TokenType.Number:
                    if (int.TryParse(token.value, out _))
                    {
                        return "int";
                    }
                    else if (float.TryParse(token.value, out _))
                    {
                        return "float";
                    }
                    else
                    {
                        throw CreateException(reader, "invalid number");
                    }
                case TokenType.Boolean:
                    return "bool";
                default:
                    throw CreateException(reader, "not implemented");
            }
        }

        static Exception CreateException(ICharReader fileReader, string message)
            => new Exception($"[Error] {fileReader.CurrentLine}:{fileReader.CurrentLineOffset} {message}");
    }

    internal static class Error
    {
        internal static Exception UnexpectedToken(ICharReader fileReader, string expected, string actual)
            => new Exception($"[Error] {fileReader.CurrentLine}:{fileReader.CurrentLineOffset}" +
                             $"expected: {expected}, found: {actual}");
    }
}