using System;
using System.IO;

namespace JsonToCsharp.OptionParsers
{
    internal partial class ConsoleOptions
    {
        internal string ClassName { get; private set; }

        private class ClassNameParser : IConsoleOptionParser
        {
            public int ArgCount => 2;
            public string[] ArgNames { get; } = {"n", "name"};

            public void Parse(ReadOnlySpan<string> args, ConsoleOptions options)
            {
                if (args.Length != 2)
                {
                    throw new Exception("write class name for input after -n");
                }

                options.ClassName = args[1];
            }

            public void SetDefaults(ConsoleOptions options)
            {
                if (options.ClassName == null)
                {
                    options.ClassName = Path.GetFileNameWithoutExtension(options.InPath.FullName);
                }
            }
        }
    }
}