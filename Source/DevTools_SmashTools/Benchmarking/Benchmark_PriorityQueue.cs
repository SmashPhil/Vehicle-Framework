using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DevTools.Benchmarking;
using Verse;

// ReSharper disable all

namespace SmashTools.Performance;

[BenchmarkClass("PriorityQueue")]
internal class Benchmark_PriorityQueue
{
  [Benchmark(Label = ".Net PriorityQueue")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void PriorityQueue_DotNet(ref PriorityQueueContext context)
  {
    PriorityQueue<byte, float> priorityQueue = new();
    for (int i = 0; i < context.items.Length; i++)
    {
      priorityQueue.Enqueue(0, context.items[i]);
    }

    while (priorityQueue.TryDequeue(out _, out _))
    {
    }
  }

  [Benchmark(Label = "FastPriorityQueue")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void PriorityQueue_Verse(ref PriorityQueueContext context)
  {
    FastPriorityQueue<float> priorityQueue = new();
    for (int i = 0; i < context.items.Length; i++)
    {
      priorityQueue.Push(context.items[i]);
    }

    while (priorityQueue.Count > 0)
    {
      priorityQueue.Pop();
    }
  }

  private readonly struct PriorityQueueContext
  {
    public readonly float[] items;

    public PriorityQueueContext()
    {
      const int ItemSize = 1000;

      items = new float[ItemSize];
      for (int i = 0; i < ItemSize; i++)
      {
        items[i] = Rand.Value;
      }
    }
  }
}