using System;
using System.Runtime.CompilerServices;
using DevTools.Benchmarking;

namespace SmashTools.Benchmarking;

[BenchmarkClass("EnumBitwise")]
internal class Benchmark_EnumBitwise
{
  [Benchmark(Label = "HasFlag")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void HasFlag(ref readonly ContainsContext context)
  {
    bool throwaway = context.a.HasFlag(context.b);
  }

  [Benchmark(Label = "Bitwise")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void Bitwise(ref readonly ContainsContext context)
  {
    // Need to capture result or function will be a no-op
    bool throwaway = (context.a & context.b) != 0;
  }

  [Benchmark(Label = "Unsafe")]
  [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
  private static void Unsafe(ref readonly ContainsContext context)
  {
    bool throwaway = context.a.IsAnyBitSet(context.b);
  }

  private readonly struct ContainsContext
  {
    public readonly TestEnum a;
    public readonly TestEnum b;

    public ContainsContext()
    {
      a = TestEnum.A;
      b = TestEnum.B | TestEnum.C;
    }
  }

  [Flags]
  private enum TestEnum
  {
    A = 1,
    B = 2,
    C = 4
  }
}