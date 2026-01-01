using System.Runtime.CompilerServices;
using DevTools.Benchmarking;

namespace SmashTools.Benchmarking;

// ReSharper disable UnusedVariable
[BenchmarkClass("IsEven")]
internal class Benchmark_IsEven
{
	[Benchmark(Label = "Modulo")]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool Modulo(ref readonly ContainsContext context)
	{
		return context.even % 2 == 0;
	}

	[Benchmark(Label = "Bitwise")]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool Bitwise(ref readonly ContainsContext context)
	{
		return (context.even & 1) == 0;
	}

	[Benchmark(Label = "BitShift")]
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool BitShift(ref readonly ContainsContext context)
	{
		return context.even >> 1 << 1 == context.even;
	}

	private readonly struct ContainsContext
	{
		public readonly int even;
		public readonly int odd;

		public ContainsContext()
		{
			even = 21234;
			odd = 10583;
		}
	}
}