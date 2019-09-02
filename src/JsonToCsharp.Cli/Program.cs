using System;
using JsonToCsharp.Core;
using JsonToCsharp.OptionParsers;

namespace JsonToCsharp
{
    using static Console;

    internal static class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintHelp();
                return;
            }

            var consoleOptions = new ConsoleOptions(args);

            using (var reader = new FileReader(consoleOptions.InPath.FullName))
            {
                var options = new Options
                {
                    NameSpace = consoleOptions.NameSpace,
                    DeclareDataMember = consoleOptions.DeclareDataMember,
                    ListType = consoleOptions.ListType
                };
                var generator = new JsonToCsharpGenerator(options);
                generator.Create(consoleOptions.ClassName, reader, consoleOptions.OutDir);
            }
        }

        private static void PrintHelp()
        {
            WriteLine("usage: json-to-csharp [options] <input>");
            WriteLine("options:");
            WriteLine("  -n    name of root object class        (default = file name)");
            WriteLine("  -s    name of namespace class resides  (default = global)");
            WriteLine("  -o    output directory path            (default = [input dir]/out)");
        }
    }
}