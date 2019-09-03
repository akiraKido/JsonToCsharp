using System;
using System.Collections.Generic;
using System.Text;

namespace JsonToCsharp.Core
{
    internal class ClassBuilder
    {
        private readonly StringBuilder _result = new StringBuilder();
        private int _indent = 0;

        internal void AddLine() => AddLine("");
        
        internal void AddLine(string line)
        {
            for (int i = 0; i < _indent; i++)
            {
                _result.Append("    ");
            }

            _result.Append(line);
            _result.Append(Environment.NewLine);
        }

        internal void AddLines(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                AddLine(line);
            }
        }

        internal void Indent() => _indent++;
        internal void Dedent() => _indent--;

        public static ClassBuilder operator +(ClassBuilder builder, string str)
        {
            builder.AddLine(str);
            return builder;
        }

        public override string ToString()
        {
            return _result.ToString();
        }
    }
}