using System.Collections.Generic;
using DevTools.Benchmarking;

// ReSharper disable all

namespace SmashTools.Performance;

[BenchmarkClass("HashSet")]
internal class Benchmark_HashSet
{
  [Benchmark(Label = "StringContainsTrue")]
  private static void StringContainsTrue(ref HashSetContext context)
  {
    context.stringHashSet.Contains(HashSetContext.containedString);
  }

  [Benchmark(Label = "StringContainsFalse")]
  private static void StringContainsFalse(ref HashSetContext context)
  {
    context.stringHashSet.Contains(HashSetContext.otherString);
  }

  [Benchmark(Label = "ObjectContainsTrue")]
  private static void ObjectContainsTrue(ref HashSetContext context)
  {
    context.objectHashSet.Contains(HashSetContext.containedObject);
  }

  [Benchmark(Label = "ObjectContainsFalse")]
  private static void ObjectContainsFalse(ref HashSetContext context)
  {
    context.objectHashSet.Contains(HashSetContext.otherObject);
  }

  [Benchmark(Label = "IntContainsTrue")]
  private static void IntContainsTrue(ref HashSetContext context)
  {
    context.intHashSet.Contains(HashSetContext.containedInt);
  }

  [Benchmark(Label = "IntContainsFalse")]
  private static void IntContainsFalse(ref HashSetContext context)
  {
    context.intHashSet.Contains(HashSetContext.otherInt);
  }

  private readonly struct HashSetContext
  {
    public static string containedString = "Dolphin";
    public static string otherString = "Tiger";
    public static object containedObject = new();
    public static object otherObject = new();
    public static int containedInt = 29;
    public static int otherInt = 71;

    public readonly HashSet<string> stringHashSet;
    public readonly HashSet<object> objectHashSet;
    public readonly HashSet<int> intHashSet;

    public HashSetContext()
    {
      stringHashSet = [containedString, "Horse, Pineapple", "Giraffe", "Wombat"];
      objectHashSet = [containedObject, new(), new(), new(), new()];
      intHashSet = [containedInt, 93, 62, 84, 15];
    }
  }
}