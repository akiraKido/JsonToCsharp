using System;
using JsonToCsharp.Core;

namespace JsonToCsharp.OptionParsers
{
    internal partial class ConsoleOptions
    {
        internal ListType ListType { get; private set; }

        private class ListNameParser : IConsoleOptionParser
        {
            public int ArgCount => 2;
            public string[] ArgNames { get; } = {"l", "listName"};

            public void Parse(ReadOnlySpan<string> args, ConsoleOptions options)
            {
                if (args.Length != 2)
                {
                    var listTypes = string.Join("/", Enum.GetNames(typeof(ListType)));
                    throw new Exception($"write list type after -l: {listTypes}");
                }

                try
                {
                    options.ListType = Enum.Parse<ListType>(args[1], false);
                }
                catch
                {
                    var listTypes = string.Join("/", Enum.GetNames(typeof(ListType)));
                    throw new Exception($"list types available: [{listTypes}]");
                }
            }

            public void SetDefaults(ConsoleOptions options)
            {
                if (options.ListType == ListType.None)
                {
                    options.ListType = ListType.IEnumerable;
                }
            }
        }
    }
}