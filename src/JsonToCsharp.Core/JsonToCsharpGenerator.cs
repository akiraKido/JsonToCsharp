using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JsonToCsharp.Core
{
    using System.Reflection;
    using static Console;

    public class JsonToCsharpGenerator
    {
        private readonly IReadOnlyOptions _options;

        public JsonToCsharpGenerator() => _options = new Options();

        public JsonToCsharpGenerator(IReadOnlyOptions options) => _options = options;

        public void Create(string name, ICharReader reader, DirectoryInfo outputDir)
        {
            using (var lexer = new Lexer(reader))
            {
                CreateEntry(name, outputDir, reader, lexer);
            }
        }

        private static readonly HashSet<string> PredefinedCsharpIdentifiers =
            new HashSet<string>(new []
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

        private string CreateEntry(string name, DirectoryInfo outputDir, ICharReader reader, Lexer lexer)
        {
            var token = lexer.Token;
            var items = new List<(string, string)>();

            if (token.tokenType != TokenType.L_Brace)
            {
                WriteLine(token.value);
                throw CreateException(reader, "expected {");
            }

            lexer.Advance(); // eat {
            while ((token = lexer.Token).tokenType != TokenType.R_Brace)
            {
                var item = GetItem(outputDir, reader, lexer);
                items.Add(item);
            }

            lexer.Advance();

            var result = CreateImmutableClass(name, items);
            var outputPath = Path.Combine(outputDir.FullName, $"{name.SnakeToUpperCamel()}.cs");
            File.WriteAllText(outputPath, result);

            return name.SnakeToUpperCamel();
        }

        private string CreateImmutableClass(string name, IReadOnlyList<(string name, string type)> items)
        {
            var nameCamelUpper = name.SnakeToUpperCamel();

            var builder = new ClassBuilder();

            builder.AddLines(new[]
            {
                "using System;",
                "using System.Collections.Generic;",
                "using System.Runtime.Serialization;",
                ""
            });

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
            foreach (var item in items.Take(items.Count - 1))
            {
                var itemName = item.name;
                if (PredefinedCsharpIdentifiers.Contains(itemName))
                {
                    itemName = $"{itemName.SnakeToUpperCamel()}";
                }

                builder += $"{item.type} {itemName},";
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


        private (string, string) GetItem(DirectoryInfo outputDir, ICharReader reader, Lexer lexer)
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
                    type = CreateEntry(key, outputDir, reader, lexer);
                    break;
                case TokenType.L_Bracket:
                    var genericType = GetArrayGenericType(key, outputDir, reader, lexer);
                    type = $"IEnumerable<{genericType}>";
                    break;
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

        private string GetArrayGenericType(string key, DirectoryInfo outputDir, ICharReader reader, Lexer lexer)
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
                CreateEntry(arrayEntryName, outputDir, reader, lexer);
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