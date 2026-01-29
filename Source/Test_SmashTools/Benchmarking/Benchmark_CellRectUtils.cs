using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DevTools.Benchmarking;
using Verse;

namespace SmashTools.Performance;

[BenchmarkClass("CellRectOverlap")]
internal class Benchmark_CellRectOverlap
{
  [Benchmark(Label = "Duplicates")]
  private static void Duplicates(ref CellRectContext context)
  {
    foreach (IntVec3 cell in context.normalRect)
    {
      DeadCodeHelper.Consume(cell.x);
    }
    foreach (IntVec3 cell in context.rotatedRect)
    {
      DeadCodeHelper.Consume(cell.x);
    }
  }

  [Benchmark(Label = "HashSet")]
  private static void HashSet(ref CellRectContext context)
  {
    context.hashset.AddRange(context.normalRect.Cells);
    context.hashset.AddRange(context.rotatedRect.Cells);

    foreach (IntVec3 cell in context.hashset)
    {
      DeadCodeHelper.Consume(cell.x);
    }

    context.hashset.Clear();
  }

  [Benchmark(Label = "Enumerator")]
  [MethodImpl(MethodImplOptions.NoOptimization)]
  private static void Enumerator(ref CellRectContext context)
  {
    foreach (IntVec3 cell in new CellRectOverlap(context.normalRect, context.rotatedRect))
    {
      DeadCodeHelper.Consume(cell.x);
    }
  }

  private readonly struct CellRectContext
  {
    public readonly CellRect normalRect;
    public readonly CellRect rotatedRect;
    public readonly HashSet<IntVec3> hashset;

    public CellRectContext()
    {
      const int Width = 3;
      const int Height = 5;
      IntVec3 testPosition = new(3, 0, 3);

      normalRect = CellRect.CenteredOn(testPosition, Width, Height);
      rotatedRect = CellRect.CenteredOn(testPosition, Height, Width);
      hashset = [];
    }
  }
}