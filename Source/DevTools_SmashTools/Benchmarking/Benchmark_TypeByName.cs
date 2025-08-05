using System.Collections.Generic;
using DevTools.Benchmarking;
using HarmonyLib;
using Verse;

namespace SmashTools.Performance;

[BenchmarkClass("TypeByName")]
internal class Benchmark_TypeByName
{
  [Benchmark(Label = "AccessTools::TypeByName")]
  private static void TypeByName_AccessTools(ref TypeContext context)
  {
    GenTypes.ClearCache();
    foreach (string typeName in context.typesToFind)
    {
      _ = AccessTools.TypeByName(typeName);
    }
  }

  [Benchmark(Label = "GenTypes::GetTypeInAnyAssembly")]
  private static void GenTypes_GetTypeInAnyAssembly(ref TypeContext context)
  {
    foreach (string typeName in context.typesToFind)
    {
      _ = GenTypes.GetTypeInAnyAssembly(typeName);
    }
  }

  private readonly struct TypeContext
  {
    public readonly List<string> typesToFind;

    public TypeContext()
    {
      typesToFind =
      [
        // Verse
        "Verse.GenTypes",
        // mscorlib
        "System.String",
        // Current executing assembly
        "SmashTools.Performance.Benchmark_TypeByName",
        // Harmony
        "HarmonyLib.AccessTools",
      ];
    }
  }
}