using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DevTools.Benchmarking;
using Verse;

// ReSharper disable all

namespace SmashTools.Performance;

[BenchmarkClass("GenTypes")]
internal class Benchmark_GenTypes
{
  [Benchmark(Label = "Parallel")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void AllTypes_Parallel()
  {
    Parallel.ForEach(GenTypes.AllTypes, (type) => { });
  }

  [Benchmark(Label = "Partitioned")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void AllTypes_Partitioned()
  {
    Parallel.ForEach(Partitioner.Create(0, GenTypes.AllTypes.Count), (range, state) =>
    {
      for (int i = range.Item1; i <= range.Item2; i++)
      {
      }
    });
  }

  [Benchmark(Label = "Sequential")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void AllTypes_Sequential()
  {
    foreach (Type type in GenTypes.AllTypes)
    {
    }
  }
}