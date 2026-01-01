using System.Collections.Generic;
using DevTools.Benchmarking;

namespace SmashTools.Benchmarking;

[BenchmarkClass("ComponentCache Collection")]
[Measurement(Benchmark.Measurement.Nanoseconds)]
internal class Benchmark_GetMapComp
{
  [Benchmark(Label = "Dictionary")]
  private static void GetMapComponentDictionary(ref GetComponentContext context)
  {
    _ = context.dict[0];
    _ = context.dict[1];
    _ = context.dict[2];
  }

  [Benchmark(Label = "Index")]
  private static void GetMapComponentIndex1(ref GetComponentContext context)
  {
    int index = context.list.IndexOf(context.obj1);
    _ = context.dict[index];
    int index2 = context.list.IndexOf(context.obj2);
    _ = context.dict[index2];
    int index3 = context.list.IndexOf(context.obj3);
    _ = context.dict[index3];
  }

  private readonly struct GetComponentContext
  {
    public readonly Dictionary<int, object> dict;
    public readonly List<object> list;

    public readonly object obj1 = new();
    public readonly object obj2 = new();
    public readonly object obj3 = new();

    public GetComponentContext()
    {
      list = [];
      dict = [];

      dict[0] = obj1;
      dict[1] = obj2;
      dict[2] = obj3;

      list.Add(obj1);
      list.Add(obj2);
      list.Add(obj3);
    }
  }
}