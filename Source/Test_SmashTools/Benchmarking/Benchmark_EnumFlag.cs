using System;
using DevTools.Benchmarking;

namespace SmashTools.Benchmarking;

[BenchmarkClass("EnumBitwise")]
internal class Benchmark_EnumBitwise
{
  public TestEnum a = TestEnum.A;
  public TestEnum b = TestEnum.B | TestEnum.C;

  [Benchmark(Label = "HasFlag")]
	public bool HasFlag()
	{
		return a.HasFlag(b);
	}

	[Benchmark(Label = "Bitwise")]
  public bool Bitwise()
	{
		return (a & b) != 0;
	}

	[Benchmark(Label = "Unsafe")]
  public bool Unsafe()
	{
		return a.IsAnyBitSet(b);
	}

	[Flags]
  public enum TestEnum
	{
		A = 1,
		B = 2,
		C = 4
	}
}