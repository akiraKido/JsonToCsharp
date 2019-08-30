using System;
using JsonToCsharp.Core;

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

            var options = new Options(args);

            using (var reader = new FileReader(options.InPath.FullName))
            {
                JsonToCsharpGenerator.Create(options.ClassName, options.NameSpace, options.OutDir, reader);
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
