using System.Collections.Generic;
using System.Linq;
using DevTools.Benchmarking;
using Verse;

namespace SmashTools.Benchmarking;

[BenchmarkClass("Contains")]
internal class Benchmark_Contains
{
  [Benchmark(Label = "ContainsEnumeration")]
  private static void ContainsEnumeration(ref readonly ContainsContext context)
  {
    foreach (int number in context.intList)
    {
      _ = context.intList2.Contains(number);
    }
  }

  [Benchmark(Label = "ContainsHashSet")]
  private static void ContainsHashSet(ref readonly ContainsContext context)
  {
    HashSet<int> hashSet = context.intList2.ToHashSet();
    foreach (int number in context.intList)
    {
      _ = hashSet.Contains(number);
    }
  }

  private readonly struct ContainsContext
  {
    public readonly List<int> intList;
    public readonly List<int> intList2;

    public ContainsContext()
    {
      const int Seed = 231456;
      const int MaxSize = 25;

      using RandBlock randBlock = new(Seed);
      intList = new List<int>(MaxSize);
      intList2 = new List<int>(MaxSize);
      for (int i = 0; i < MaxSize; i++)
      {
        int number = Rand.Int;
        intList.Add(number);
        intList2.Add(number);
      }
      intList2.Shuffle();
    }
  }
}