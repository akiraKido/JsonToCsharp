using System;
using System.IO;

namespace JsonToCsharp.OptionParsers
{
    internal partial class ConsoleOptions
    {
        internal DirectoryInfo OutDir { get; private set; }

        private class OutputDirectoryParser : IConsoleOptionParser
        {
            public int ArgCount => 2;
            public string[] ArgNames { get; } = {"o", "output"};

            public void Parse(ReadOnlySpan<string> args, ConsoleOptions options)
            {
                if (args.Length != 2)
                {
                    throw new Exception("write directory name for output after -o");
                }

                options.OutDir = new DirectoryInfo(Path.GetFullPath(args[1]));
                if (options.OutDir.Exists == false)
                {
                    options.OutDir.Create();
                }
            }

            public void SetDefaults(ConsoleOptions options)
            {
                if (options.OutDir != null) return;

                options.OutDir = new DirectoryInfo(Path.Combine(options.InPath.DirectoryName, "out"));
                if (options.OutDir.Exists == false)
                {
                    options.OutDir.Create();
                }
            }
        }
    }
}