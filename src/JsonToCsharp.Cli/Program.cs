using System;
using System.IO;
using System.Threading.Tasks;
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

            var generatorOptions = new Options
            {
                NameSpace = consoleOptions.NameSpace,
                DeclareDataMember = consoleOptions.DeclareDataMember,
                ListType = consoleOptions.ListType
            };
            var generator = new JsonToCsharpGenerator(generatorOptions);

            var task = Task.Run(() =>
            {
                using (var reader = new FileReader(consoleOptions.InPath.FullName))
                {
                    var classResults = generator.Create(consoleOptions.ClassName, reader);
                    
                    foreach (var (className, generatedText) in  classResults)
                    {
                        var outputPath = Path.Combine(consoleOptions.OutDir.FullName, $"{className}.cs");
                        File.WriteAllText(outputPath, generatedText);
                    }
                }
            });

            char[] rotation = { '/', '-', '\\', '|', '/', '-', '\\', '|' };
            int i = 0;
            while (task.IsCompleted == false)
            {
                SetCursorPosition(0, 0);
                Write($"[{rotation[i]}] Executing");
                i = (i + 1) % rotation.Length;
            }
            WriteLine();
            WriteLine("Complete!");
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