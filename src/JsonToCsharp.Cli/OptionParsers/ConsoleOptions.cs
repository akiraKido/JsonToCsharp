using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JsonToCsharp.OptionParsers
{
    internal interface IConsoleOptionParser
    {
        int ArgCount { get; }

        string[] ArgNames { get; }

        void Parse(ReadOnlySpan<string> args, ConsoleOptions options);

        void SetDefaults(ConsoleOptions options);
    }

    internal partial class ConsoleOptions
    {
        internal FileInfo InPath { get; }

        private readonly IReadOnlyList<IConsoleOptionParser> _parsers = new IConsoleOptionParser[]
        {
            new ClassNameParser(),
            new NamespaceParser(),
            new OutputDirectoryParser(),
            new DeclareDataMemberParser(),
            new ListNameParser(),
        };

        internal ConsoleOptions(string[] args)
        {
            var offset = 0;
            while (offset < args.Length)
            {
                var arg = args[offset];
                if (arg[0] != '-')
                {
                    // default argument
                    if (InPath != null)
                    {
                        throw new Exception($"invalid argument {arg}");
                    }

                    InPath = new FileInfo(Path.GetFullPath(arg));
                    offset += 1;
                    continue;
                }

                var argName = args[offset].Substring(1); // remove - from input
                var parser = _parsers.FirstOrDefault(p => p.ArgNames.Contains(argName));
                if (parser == null)
                {
                    throw new Exception($"invalid parameter: {arg}");
                }

                var span = new Span<string>(args, offset, parser.ArgCount);
                parser.Parse(span, this);

                offset += parser.ArgCount;
            }

            foreach (var parser in _parsers)
            {
                parser.SetDefaults(this);
            }
        }
    }
}