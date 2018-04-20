using System;
using System.IO;

namespace JsonToCsharp
{
    internal class Options
    {
        internal FileInfo InPath {get;}
        internal DirectoryInfo OutDir {get;}
        internal string ClassName {get;}
        internal string NameSpace {get;}

        internal Options(string[] args)
        {
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-name":
                    case "-n":
                        i++;
                        if (i >= args.Length)
                        {
                            throw new Exception("write file name for input after -n");
                        }
                        ClassName = args[i];
                        break;
                    case "-namespace":
                    case "-s":
                        i++;
                        if (i >= args.Length)
                        {
                            throw new Exception("write namespace name after -n");
                        }
                        NameSpace = args[i];
                        break;
                    case "-output":
                    case "-o":
                        i++;
                        if (i >= args.Length)
                        {
                            throw new Exception("write directory name for output after -o");
                        }
                        OutDir = new DirectoryInfo(Path.GetFullPath(args[i]));
                        break;
                    default:
                        if(InPath != null)
                        {
                            throw new Exception("invalid argument");
                        }
                        InPath = new FileInfo(Path.GetFullPath(args[i]));
                        break;
                }
            }

            if (ClassName == null)
            {
                ClassName = Path.GetFileNameWithoutExtension(InPath.FullName);
            }

            if (OutDir == null)
            {
                OutDir = new DirectoryInfo(Path.Combine(InPath.DirectoryName, "out"));
            }
            
            if(OutDir.Exists)
            {
                OutDir.Delete(true);
            }
            OutDir.Create();
        }
    }
}