using DevTools.Benchmarking;
using Verse;

namespace SmashTools.Performance;

[BenchmarkClass("TypeCheck"), SampleSize(1_000_000)]
internal class Benchmark_TypeCheck
{
  [Benchmark(Label = "IsSubclass")]
  private static void TypeCheck_IsSubclass()
  {
    _ = typeof(ThingDef).IsSubclassOf(typeof(Def));
  }

  [Benchmark(Label = "IsAssignableFrom")]
  private static void TypeCheck_IsAssignableFrom()
  {
    _ = typeof(Def).IsAssignableFrom(typeof(ThingDef));
  }

  [Benchmark(Label = "IsSubclass Generic")]
  private static void TypeCheck_IsSubclassGeneric()
  {
    _ = typeof(ThingOwner<Thing>).IsSubclassOf(typeof(ThingOwner));
  }

  [Benchmark(Label = "IsAssignableFrom Generic")]
  private static void TypeCheck_IsAssignableFromGeneric()
  {
    _ = typeof(ThingOwner).IsAssignableFrom(typeof(ThingOwner<Thing>));
  }
}