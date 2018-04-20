using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JsonToCsharp
{
    using System.Reflection;
    using static Console;
    using static Environment;

    internal static class Error
    {
        internal static Exception UnexpectedToken(ICharReader fileReader, string expected, string actual)
            => new Exception($"[Error] {fileReader.CurrentLine}:{fileReader.CurrentLineOffset}" + 
                             $"expected: {expected}, found: {actual}");
    }

    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintHelp();
                return;
            }

            var options = new Options(args);

            var items = new List<(string name, string type)>();

            using (var reader = new FileReader(options.InPath.FullName))
            using (var lexer = new Lexer(reader))
            {
                (TokenType tokenType, string value) token;
                while ((token = lexer.Token).tokenType != TokenType.EndOfFile)
                {
                    CreateEntry(options.ClassName, options.NameSpace, options.OutDir, reader, lexer);
                }
            }
        }

        static string CreateEntry(string name, string nameSpace, DirectoryInfo outputDir, ICharReader reader, Lexer lexer)
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
                var item = GetItem(nameSpace, outputDir, reader, lexer);
                items.Add(item);
            }
            lexer.Advance();

            var result = CreateImmutableClass(name, nameSpace, items);
            var outputPath = Path.Combine(outputDir.FullName, $"{name.SnakeToUpperCamel()}.cs");
            File.WriteAllText(outputPath, result);

            return name.SnakeToUpperCamel();
        }

        class ClassBuilder
        {
            private readonly StringBuilder _result = new StringBuilder();
            private int _indent = 0;

            internal void AddLine(string line)
            {
                for (int i = 0; i < _indent; i++)
                {
                    _result.Append("    ");
                }
                _result.Append(line);
                _result.Append(NewLine);
            }

            internal void Indent() => _indent++;
            internal void Dedent() => _indent--;

            public static ClassBuilder operator +(ClassBuilder builder, string str)
            {
                builder.AddLine(str);
                return builder;
            }

            public override string ToString()
            {
                return _result.ToString();
            }
        }


        private static readonly HashSet<string> PredefinedCsharpIdentifiers =
            File.ReadLines(
                Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    "keywords-csharp.txt"
                )
            ).ToHashSet();

        static string CreateImmutableClass(string name, string nameSpace, IReadOnlyList<(string name, string type)> items)
        {
            var nameCamelUpper = name.SnakeToUpperCamel();

            var builder = new ClassBuilder();
            
            builder += "using System;";
            builder += "using System.Collections.Generic;";
            builder += "using System.Runtime.Serialization;";
            builder += $"";
            if (string.IsNullOrWhiteSpace(nameSpace) == false)
            {
                builder += $"namespace {nameSpace}";
                builder += $"{{";
                builder.Indent();
            }
            builder += $"public class {nameCamelUpper}";
            builder += $"{{";
            builder.Indent();
            builder += $"public {nameCamelUpper}";
            builder += $"(";
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
                builder += $"[DataMember(Name = \"{item.name}\")]";
                builder += $"public {item.type} {item.name.SnakeToUpperCamel()} {{ get; }}";
            }
            builder.Dedent();
            builder += $"}}";

            if (string.IsNullOrWhiteSpace(nameSpace) == false)
            {
                builder.Dedent();
                builder += $"}}";
            }

            return builder.ToString();
        }

        static (string, string) GetItem(string nameSpace, DirectoryInfo outputDir, ICharReader reader, Lexer lexer)
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
                    type = CreateEntry(key, nameSpace, outputDir, reader, lexer);
                    break;
                case TokenType.L_Bracket:
                    var genericType = GetArrayGenericType(key, nameSpace, outputDir, reader, lexer);
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

        private static string GetArrayGenericType(string key, string nameSpace, DirectoryInfo outputDir, ICharReader reader, Lexer lexer)
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
                CreateEntry(arrayEntryName, nameSpace, outputDir, reader, lexer);
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
                    if(DateTime.TryParse(token.value, out _))
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


        private static void PrintHelp()
        {
            WriteLine("usage: json-to-csharp [options] <input>");
            WriteLine("options:");
            WriteLine("  -n    name of root object class");
            WriteLine("  -o    output directory path");
        }
    }

}
