using DevTools.Benchmarking;
using Verse;

namespace SmashTools.Performance;

[BenchmarkClass("KeyPrefs")]
internal class Benchmark_KeyPrefs
{
	[Benchmark(Label = "DirectXmlLoader")]
	private static void DirectXmlLoader()
	{
		KeyPrefs.Init();
	}

	[Benchmark(Label = "KeyPrefsLoader")]
	private static void KeyPrefsLoaderInit()
	{
		KeyPrefsLoader.Init();
	}
}