using System;
using System.Linq;

namespace JsonToCsharp
{
    internal static class StringExtensions
    {
        internal static string SnakeToUpperCamel(this string self)
        {
            if (string.IsNullOrEmpty(self)) return self;

            return self
                .Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1, s.Length - 1))
                .Aggregate(string.Empty, (s1, s2) => s1 + s2)
            ;
        }
    }
}