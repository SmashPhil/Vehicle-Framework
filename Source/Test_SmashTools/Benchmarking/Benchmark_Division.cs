using System.Runtime.CompilerServices;
using DevTools.Benchmarking;

namespace SmashTools.Benchmarking;

// ReSharper disable UnusedVariable
[BenchmarkClass("Division")]
internal class Benchmark_Division
{
  [Benchmark(Label = "Divisor")]
  [MethodImpl(MethodImplOptions.NoOptimization)]
  private static int Divisor(ref readonly DivisionContext context)
  {
    int numerator = context.numerator;
    int denominator = context.divisor;

    int sum = 0;
    for (int i = 0; i < 10000; i++)
    {
      _ = numerator / denominator;
    }
    return sum;
  }
  
  [Benchmark(Label = "BitShift")]
  [MethodImpl(MethodImplOptions.NoOptimization)]
  private static int BitShift(ref readonly DivisionContext context)
  {
    int numerator = context.numerator;
    ulong factor = context.factor;

    int sum = 0;
    for (int i = 0; i < 10000; i++)
    {
      ulong prod = (ulong)numerator * factor;
      _ = (int)((prod + (1UL << 31)) >> 32);
    }
    return sum;
  }

  private readonly struct DivisionContext
  {
    public readonly int numerator;
    public readonly int divisor;

    public readonly ulong factor;

    public DivisionContext()
    {
      numerator = 14352;
      divisor = 15;
      factor = (1UL << 32) / (ulong)divisor;
    }
  }
}