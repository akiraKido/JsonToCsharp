using System;

namespace JsonToCsharp.OptionParsers
{
    internal partial class ConsoleOptions
    {
        internal string NameSpace { get; private set; }

        private class NamespaceParser : IConsoleOptionParser
        {
            public int ArgCount => 2;
            public string[] ArgNames { get; } = {"s", "namespace"};

            public void Parse(ReadOnlySpan<string> args, ConsoleOptions options)
            {
                if (args.Length != 2)
                {
                    throw new Exception("write namespace name after -n");
                }

                options.NameSpace = args[1];
            }

            public void SetDefaults(ConsoleOptions options) => options.NameSpace = null;
        }
    }
}