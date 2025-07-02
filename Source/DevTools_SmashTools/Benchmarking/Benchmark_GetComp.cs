using System;
using System.Collections.Generic;
using System.Linq;
using DevTools.Benchmarking;
using RimWorld;
using Verse;

// ReSharper disable ForCanBeConvertedToForeach
namespace SmashTools.Performance;

[BenchmarkClass("GetComp"), SampleSize(1_000_000)]
[Measurement(Benchmark.Measurement.Nanoseconds)]
internal class Benchmark_GetComp
{
  private const int CountSingle = 1;
  private const int CountFew = 3;
  private const int CountMany = 8;

  private static readonly Type[] CompTypes =
  [
    typeof(CompForbiddable),
    typeof(CompArt),
    typeof(CompPowerBattery),
    typeof(CompExplosive),
    typeof(CompAtomizer),
    typeof(CompBiocodable),
    typeof(CompDeepDrill),
    typeof(CompStatue)
  ];

  // 1 Item
  [Benchmark(Label = "List 1")]
  private static void GetComp_List_1_HEAD(ref GetCompContext context)
  {
    _ = GetList<CompForbiddable>(context.listSingle);
  }

  [Benchmark(Label = "Dictionary 1")]
  private static void GetComp_Dictionary_1_HEAD(ref GetCompContext context)
  {
    _ = GetDictionary<CompForbiddable>(context.dictionarySingle);
  }

  [Benchmark(Label = "Sorted 1")]
  private static void GetComp_Sorted_1_HEAD(ref GetCompContext context)
  {
    _ = GetSorted<CompForbiddable>(context.sortedSingle);
  }


  // 3 Items - Match at head of list
  [Benchmark(Label = "List 3 Head")]
  private static void GetComp_List_3_HEAD(ref GetCompContext context)
  {
    _ = GetList<CompForbiddable>(context.listFew);
  }

  [Benchmark(Label = "Dictionary 3 Head")]
  private static void GetComp_Dictionary_3_HEAD(ref GetCompContext context)
  {
    _ = GetDictionary<CompForbiddable>(context.dictionaryFew);
  }

  [Benchmark(Label = "Sorted 3 Head")]
  private static void GetComp_Sorted_3_HEAD(ref GetCompContext context)
  {
    _ = GetSorted<CompForbiddable>(context.sortedFew);
  }


  // 3 Items - Match at tail of list
  [Benchmark(Label = "List 3 Tail")]
  private static void GetComp_List_3_TAIL(ref GetCompContext context)
  {
    _ = GetList<CompPowerBattery>(context.listFew);
  }

  [Benchmark(Label = "Dictionary 3 Tail")]
  private static void GetComp_Dictionary_3_TAIL(ref GetCompContext context)
  {
    _ = GetDictionary<CompPowerBattery>(context.dictionaryFew);
  }

  [Benchmark(Label = "Sorted 3 Tail")]
  private static void GetComp_Sorted_3_TAIL(ref GetCompContext context)
  {
    _ = GetSorted<CompPowerBattery>(context.sortedFew);
  }


  // 3 Items - Average time to search every item in the list
  [Benchmark(Label = "List 3 Average")]
  private static void GetComp_List_3_AVERAGE(ref GetCompContext context)
  {
    _ = GetList<CompForbiddable>(context.listFew);
    _ = GetList<CompArt>(context.listFew);
    _ = GetList<CompPowerBattery>(context.listFew);
  }

  [Benchmark(Label = "Dictionary 3 Average")]
  private static void GetComp_Dictionary_3_AVERAGE(ref GetCompContext context)
  {
    _ = GetDictionary<CompForbiddable>(context.dictionaryFew);
    _ = GetDictionary<CompArt>(context.dictionaryFew);
    _ = GetDictionary<CompPowerBattery>(context.dictionaryFew);
  }

  [Benchmark(Label = "Sorted 3 Average")]
  private static void GetComp_Sorted_3_AVERAGE(ref GetCompContext context)
  {
    _ = GetSorted<CompForbiddable>(context.sortedFew);
    _ = GetSorted<CompArt>(context.sortedFew);
    _ = GetSorted<CompPowerBattery>(context.sortedFew);
  }


  // 8 Items - Match at head of list
  [Benchmark(Label = "List 8 Head")]
  private static void GetComp_List_8_HEAD(ref GetCompContext context)
  {
    _ = GetList<CompForbiddable>(context.listMany);
  }

  [Benchmark(Label = "Dictionary 8 Head")]
  private static void GetComp_Dictionary_8_HEAD(ref GetCompContext context)
  {
    _ = GetDictionary<CompForbiddable>(context.dictionaryMany);
  }

  [Benchmark(Label = "Sorted 8 Head")]
  private static void GetComp_Sorted_8_HEAD(ref GetCompContext context)
  {
    _ = GetSorted<CompForbiddable>(context.sortedMany);
  }

  // 8 Items - Match at tail of list
  [Benchmark(Label = "List 8 Tail")]
  private static void GetComp_List_8_TAIL(ref GetCompContext context)
  {
    _ = GetList<CompStatue>(context.listMany);
  }

  [Benchmark(Label = "Dictionary 8 Tail")]
  private static void GetComp_Dictionary_8_TAIL(ref GetCompContext context)
  {
    _ = GetDictionary<CompStatue>(context.dictionaryMany);
  }

  [Benchmark(Label = "Sorted 8 Tail")]
  private static void GetComp_Sorted_8_TAIL(ref GetCompContext context)
  {
    _ = GetSorted<CompStatue>(context.sortedMany);
  }


  // 8 Items - Average time to search every item in the list
  [Benchmark(Label = "List 8 Average"), DivideBy(8)]
  private static void GetComp_List_8_AVERAGE(ref GetCompContext context)
  {
    _ = GetList<CompForbiddable>(context.listMany);
    _ = GetList<CompArt>(context.listMany);
    _ = GetList<CompPowerBattery>(context.listMany);
    _ = GetList<CompExplosive>(context.listMany);
    _ = GetList<CompAtomizer>(context.listMany);
    _ = GetList<CompBiocodable>(context.listMany);
    _ = GetList<CompDeepDrill>(context.listMany);
    _ = GetList<CompStatue>(context.listMany);
  }

  [Benchmark(Label = "Dictionary 8 Average")]
  private static void GetComp_Dictionary_8_AVERAGE(ref GetCompContext context)
  {
    _ = GetDictionary<CompForbiddable>(context.dictionaryMany);
    _ = GetDictionary<CompArt>(context.dictionaryMany);
    _ = GetDictionary<CompPowerBattery>(context.dictionaryMany);
    _ = GetDictionary<CompExplosive>(context.dictionaryMany);
    _ = GetDictionary<CompAtomizer>(context.dictionaryMany);
    _ = GetDictionary<CompBiocodable>(context.dictionaryMany);
    _ = GetDictionary<CompDeepDrill>(context.dictionaryMany);
    _ = GetDictionary<CompStatue>(context.dictionaryMany);
  }

  [Benchmark(Label = "Sorted 8 Average")]
  private static void GetComp_Sorted_8_AVERAGE(ref GetCompContext context)
  {
    _ = GetSorted<CompForbiddable>(context.sortedMany);
    _ = GetSorted<CompArt>(context.sortedMany);
    _ = GetSorted<CompPowerBattery>(context.sortedMany);
    _ = GetSorted<CompExplosive>(context.sortedMany);
    _ = GetSorted<CompAtomizer>(context.sortedMany);
    _ = GetSorted<CompBiocodable>(context.sortedMany);
    _ = GetSorted<CompDeepDrill>(context.sortedMany);
    _ = GetSorted<CompStatue>(context.sortedMany);
  }


  // Traditional vanilla implementation
  private static T GetList<T>(List<ThingComp> compList) where T : ThingComp
  {
    for (int i = 0; i < compList.Count; i++)
    {
      if (compList[i] is T)
        return (T)compList[i];
    }
    return null;
  }

  // Current vanilla implementation
  private static T GetDictionary<T>(Dictionary<Type, ThingComp> compsByType) where T : ThingComp
  {
    if (compsByType.TryGetValue(typeof(T), out ThingComp comp))
    {
      return (T)comp;
    }
    return null;
  }

  // SmashTools implementation
  private static T GetSorted<T>(SelfOrderingList<ThingComp> comps) where T : ThingComp
  {
    for (int i = 0; i < comps.Count; i++)
    {
      if (comps[i] is T t)
      {
        comps.Touch(i);
        return t;
      }
    }
    return null;
  }

  private readonly struct GetCompContext
  {
    public readonly List<ThingComp> listSingle;
    public readonly List<ThingComp> listFew;
    public readonly List<ThingComp> listMany;

    public readonly Dictionary<Type, ThingComp> dictionarySingle;
    public readonly Dictionary<Type, ThingComp> dictionaryFew;
    public readonly Dictionary<Type, ThingComp> dictionaryMany;

    public readonly SelfOrderingList<ThingComp> sortedSingle;
    public readonly SelfOrderingList<ThingComp> sortedFew;
    public readonly SelfOrderingList<ThingComp> sortedMany;

    public GetCompContext()
    {
      listSingle = Create(CountSingle).ToList();
      listFew = Create(CountFew).ToList();
      listMany = Create(CountMany).ToList();

      dictionarySingle = Create(CountSingle).ToDictionary(comp => comp.GetType());
      dictionaryFew = Create(CountFew).ToDictionary(comp => comp.GetType());
      dictionaryMany = Create(CountMany).ToDictionary(comp => comp.GetType());

      sortedSingle = [.. Create(CountSingle)];
      sortedFew = [.. Create(CountFew)];
      sortedMany = [.. Create(CountMany)];
    }

    private static IEnumerable<ThingComp> Create(int comps)
    {
      for (int i = 0; i < comps; i++)
      {
        yield return (ThingComp)Activator.CreateInstance(CompTypes[i]);
      }
    }
  }
}