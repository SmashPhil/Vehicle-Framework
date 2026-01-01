using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DevTools.Benchmarking;
using Verse;

namespace SmashTools.Performance;

[BenchmarkClass("CellRectUtils")]
internal class Benchmark_CellRectUtils
{
  [Benchmark(Label = "HashSet")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void CellRectUtils_HashSet(ref CellRectContext context)
  {
    context.hashset.AddRange(context.normalRect.Cells);
    context.hashset.AddRange(context.rotatedRect.Cells);

    foreach (IntVec3 _ in context.hashset)
    {
    }

    context.hashset.Clear();
  }

  [Benchmark(Label = "Extension")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void CellRectUtils_Extension(ref CellRectContext context)
  {
    foreach (IntVec3 _ in context.normalRect.AllCellsNoRepeat(context.rotatedRect))
    {
    }
  }

  private readonly struct CellRectContext
  {
    public readonly CellRect normalRect;
    public readonly CellRect rotatedRect;
    public readonly HashSet<IntVec3> hashset;

    public CellRectContext()
    {
      const int width = 3;
      const int height = 5;
      IntVec3 testPosition = new(3, 0, 3);

      normalRect = CellRect.CenteredOn(testPosition, width, height);
      rotatedRect = CellRect.CenteredOn(testPosition, height, width);
      hashset = [];
    }
  }
}