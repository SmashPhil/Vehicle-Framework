using System;
using DevTools.Benchmarking;

namespace SmashTools.Benchmarking;

[BenchmarkClass("EnumBitwise")]
internal class Benchmark_EnumBitwise
{
	[Benchmark(Label = "HasFlag")]
	private static bool HasFlag(ref readonly ContainsContext context)
	{
		return context.a.HasFlag(context.b);
	}

	[Benchmark(Label = "Bitwise")]
	private static bool Bitwise(ref readonly ContainsContext context)
	{
		return (context.a & context.b) != 0;
	}

	[Benchmark(Label = "Unsafe")]
	private static bool Unsafe(ref readonly ContainsContext context)
	{
		return context.a.IsAnyBitSet(context.b);
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