using UnityEngine.Assertions;
using Verse;

namespace SmashTools.Benchmarking;

//[BenchmarkClass("ComponentCache", AllowedGameStates = AllowedGameStates.PlayingOnMap),
// SampleSize(1_000_000)]
//[Measurement(Benchmark.Measurement.Nanoseconds)]
internal class Benchmark_ComponentCache
{
  private const int CountSingle = 1;
  private const int CountFew = 3;
  private const int CountMany = 8;

  private readonly struct GetComponentContext
  {
    private readonly Map map;

    public GetComponentContext()
    {
      this.map = Find.CurrentMap;
      Assert.IsNotNull(map);
    }
  }
}