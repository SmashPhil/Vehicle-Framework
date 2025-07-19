using System.Collections.Generic;
using DevTools.Benchmarking;
using Verse;

namespace SmashTools.Benchmarking;

[BenchmarkClass("ComponentCache"), SampleSize(100_000)]
[Measurement(Benchmark.Measurement.Nanoseconds)]
internal class Benchmark_ComponentCache
{
  [Benchmark(Label = "Raw")]
  private static void GetComponentRaw(ref GetComponentContext context)
  {
    _ = TestCache<TestObject>.GetComponent(context.map);
  }

  [Benchmark(Label = "Static Field")]
  private static void GetComponentWithField(ref GetComponentContext context)
  {
    _ = TestCache<TestObject>.GetComponentWithField(context.map);
  }

  [Benchmark(Label = "Raw Random Even")]
  private static void GetComponentRawRandomEven(ref GetComponentContext context)
  {
    foreach (int index in context.indicesEven)
    {
      _ = TestCache<TestObject>.GetComponent(context.allMaps[index]);
    }
  }

  [Benchmark(Label = "Static Field Random Even")]
  private static void GetComponentWithFieldRandomEven(ref GetComponentContext context)
  {
    foreach (int index in context.indicesEven)
    {
      _ = TestCache<TestObject>.GetComponentWithField(context.allMaps[index]);
    }
  }

  [Benchmark(Label = "Raw Random Hot")]
  private static void GetComponentRawRandomHot(ref GetComponentContext context)
  {
    foreach (int index in context.indicesHot)
    {
      _ = TestCache<TestObject>.GetComponent(context.allMaps[index]);
    }
  }

  [Benchmark(Label = "Static Field Random Hot")]
  private static void GetComponentWithFieldRandomHot(ref GetComponentContext context)
  {
    foreach (int index in context.indicesHot)
    {
      _ = TestCache<TestObject>.GetComponentWithField(context.allMaps[index]);
    }
  }

  private readonly struct GetComponentContext
  {
    public readonly MockMap map;
    public readonly MockMap map2;
    public readonly MockMap map3;

    public readonly List<MockMap> allMaps = [];
    public readonly List<int> indicesEven = [];
    public readonly List<int> indicesHot = [];

    public GetComponentContext()
    {
      const int RandomCount = 100;
      const int RandomHot = 80;
      const int Seed = 151283;

      map = new MockMap();
      map2 = new MockMap();
      map3 = new MockMap();
      allMaps.Add(map);
      allMaps.Add(map2);
      allMaps.Add(map3);

      using RandBlock rand = new(Seed);
      for (int i = 0; i < RandomCount; i++)
      {
        indicesEven.Add(i % 3);
      }
      indicesEven.Shuffle();

      for (int i = 0; i < RandomHot; i++)
        indicesHot.Add(0);
      for (int i = 0; i < RandomCount - RandomHot; i++)
      {
        indicesHot.Add((i % 2) + 1);
      }
      indicesHot.Shuffle();
    }
  }

  private static class TestCache<T> where T : BaseObject
  {
    private static T lastAccessed;
    private static readonly Dictionary<int, T> MapComps = [];

    public static T GetComponent(MockMap map)
    {
      if (!MapComps.TryGetValue(map.uniqueID, out T component))
      {
        component = map.GetComponent<T>();
        MapComps[map.uniqueID] = component;
      }
      return component;
    }

    public static T GetComponentWithField(MockMap map)
    {
      if (lastAccessed != null && lastAccessed.map.uniqueID == map.uniqueID)
        return lastAccessed;

      if (!MapComps.TryGetValue(map.uniqueID, out T component))
      {
        component = map.GetComponent<T>();
        MapComps[map.uniqueID] = component;
      }
      lastAccessed = component;
      return component;
    }
  }

  internal class BaseObject
  {
    public MockMap map;
  }

  private class TestObject : BaseObject
  {
  }

  internal class MockMap
  {
    public static int nextId;

    public readonly int uniqueID;
    public readonly BaseObject obj;

    public MockMap()
    {
      uniqueID = nextId++;
      obj = new TestObject();
      obj.map = this;
    }

    public T GetComponent<T>() where T : BaseObject
    {
      return obj as T;
    }
  }
}