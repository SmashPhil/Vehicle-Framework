using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DevTools.Benchmarking;

// ReSharper disable all

namespace Vehicles.Benchmarking;

[BenchmarkClass("Clear PathFind Nodes")]
internal class Benchmark_MapGrid
{
  [Benchmark(Label = "Parallel")]
  [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
  private static void MapGrid_Parallel(ref NodeGridContext context)
  {
    int[] grid = context.grid;
    Parallel.For(0, context.grid.Length, delegate(int index) { grid[index]++; });
  }

  [Benchmark(Label = "Partitioned")]
  [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
  private static void MapGrid_Partitioned(ref NodeGridContext context)
  {
    int[] grid = context.grid;
    Parallel.ForEach(Partitioner.Create(0, grid.Length), (range, _) =>
    {
      for (int i = range.Item1; i < range.Item2; i++)
      {
        grid[i]++;
      }
    });
  }

  [Benchmark(Label = "Sequential")]
  [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
  private static void MapGrid_Sequential(ref NodeGridContext context)
  {
    int[] grid = context.grid;
    for (int i = 0; i < context.grid.Length; i++)
    {
      grid[i]++;
    }
  }

  private readonly struct NodeGridContext
  {
    public readonly int[] grid;

    public NodeGridContext()
    {
      const int MapSize = 250;
      grid = new int[MapSize * MapSize];
    }
  }
}