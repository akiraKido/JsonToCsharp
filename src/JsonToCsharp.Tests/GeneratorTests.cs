using System;
using JsonToCsharp.Core;
using Xunit;

namespace JsonToCsharp.Tests
{
    public class GeneratorTests
    {
        [Fact]
        public void SnakeToUpperCamel_ConvertsSnakeCase()
        {
            var ext = typeof(JsonToCsharpGenerator).Assembly
                .GetType("JsonToCsharp.Core.StringExtensions");
            var method = ext!.GetMethod("SnakeToUpperCamel",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            var result = method!.Invoke(null, new object[] { "hello_world" });
            Assert.Equal("HelloWorld", result);
        }

        [Fact]
        public void Generator_CreatesSimpleClass()
        {
            var json = "{\"hoge\": 1}";
            var generator = new JsonToCsharpGenerator();
            using var reader = new MemoryReader(json);
            var result = generator.Create("TestClass", reader)["TestClass"].TrimEnd();

            var expected = string.Join(Environment.NewLine, new[]
            {
                "using System;",
                string.Empty,
                "public class TestClass",
                "{",
                "    public TestClass",
                "    (",
                "        int hoge",
                "    )",
                "    {",
                "        this.Hoge = hoge;",
                "    }",
                "    public int Hoge { get; }",
                "}"
            });

            Assert.Equal(expected, result);
        }

        [Fact]
        public void Generator_UsesEnumerableListType()
        {
            var json = "{\"numbers\": [1,2]}";
            var options = new Options { ListType = ListType.IEnumerable };
            var generator = new JsonToCsharpGenerator(options);
            using var reader = new MemoryReader(json);
            var result = generator.Create("TestClass", reader)["TestClass"];

            Assert.Contains("IEnumerable<int> numbers", result);
            Assert.Contains("public IEnumerable<int> Numbers", result);
        }
    }
}
