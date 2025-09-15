using DevTools.Benchmarking;
using Verse;

namespace SmashTools.Performance;

[BenchmarkClass("TypeCheck")]
internal class Benchmark_TypeCheck
{
	[Benchmark(Label = "IsSubclass")]
	private static bool TypeCheck_IsSubclass()
	{
		return typeof(ThingDef).IsSubclassOf(typeof(Def));
	}

	[Benchmark(Label = "IsAssignableFrom")]
	private static bool TypeCheck_IsAssignableFrom()
	{
		return typeof(Def).IsAssignableFrom(typeof(ThingDef));
	}

	[Benchmark(Label = "IsSubclass Generic")]
	private static bool TypeCheck_IsSubclassGeneric()
	{
		return typeof(ThingOwner<Thing>).IsSubclassOf(typeof(ThingOwner));
	}

	[Benchmark(Label = "IsAssignableFrom Generic")]
	private static bool TypeCheck_IsAssignableFromGeneric()
	{
		return typeof(ThingOwner).IsAssignableFrom(typeof(ThingOwner<Thing>));
	}
}