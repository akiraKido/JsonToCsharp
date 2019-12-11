# JsonToCsharp

Try it out [here](http://json2csharp.tsuchigoe.com/) (temporary URL, could change in the future).

Convert this

```csharp
{"hoge": 1}
```

to this

```csharp
using System;
public class TestClass
{
    public TestClass
    (
        int hoge
    )
    {
        this.Hoge = hoge;
    }
    public int Hoge { get; }
}
```
