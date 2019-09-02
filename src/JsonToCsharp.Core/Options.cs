using System;
using System.IO;

namespace JsonToCsharp.Core
{
    public enum ListType
    {
        None,
        IEnumerable,
        IReadOnlyList
    }

    public interface IReadOnlyOptions
    {
        string NameSpace { get; }
        bool DeclareDataMember { get; }
        ListType ListType { get; }
    }

    public class Options : IReadOnlyOptions
    {
        public string NameSpace { get; set; }
        public bool DeclareDataMember { get; set; }
        public ListType ListType { get; set; }
    }
}