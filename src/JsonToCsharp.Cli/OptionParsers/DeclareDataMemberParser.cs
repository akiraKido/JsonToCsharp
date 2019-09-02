using System;

namespace JsonToCsharp.OptionParsers
{
    internal partial class ConsoleOptions
    {
        internal bool DeclareDataMember { get; private set; }

        private class DeclareDataMemberParser : IConsoleOptionParser
        {
            public int ArgCount => 1;
            public string[] ArgNames { get; } = {"d", "declareDetaMember"};

            public void Parse(ReadOnlySpan<string> args, ConsoleOptions options)
            {
                options.DeclareDataMember = true;
            }

            public void SetDefaults(ConsoleOptions options)
            {
                options.DeclareDataMember = false;
            }
        }
    }
}