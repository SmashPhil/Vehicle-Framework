using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SmashTools;

namespace CoreLib.Performance;
// Simple counter for tracking object instantiation since RimWorld doesn't work
// well with memory profilers. This is only useful for tracking what might be
// putting pressure on GC. This is not a supplement for proper memory profilers,
// and obviously it can't find memory leaks.
public static class ObjectCounter
{
  private static readonly ConcurrentDictionary<Type, int> Counter = [];

  private static readonly ConcurrentDictionary<Type, int> CountWatched = [];

  public static bool Clear<T>() => Counter.TryRemove(typeof(T), out _);

  public static void ClearAll() => Counter.Clear();

  public static void Increment<T>()
  {
    if (!Counter.ContainsKey(typeof(T)))
    {
      Counter[typeof(T)] = 1;
    }
    else
    {
      Counter[typeof(T)]++;
    }
  }

  public static void LogAll()
  {
    foreach (Type type in Counter.Keys)
    {
      Log(type);
    }
  }

  public static void Log(Type type)
  {
    int count = Counter.GetValueOrDefault(type, defaultValue: 0);
    Logger.Message($"{type.Name} = {count}");
  }

  public static void StartWatcher<T>()
  {
    int current = Counter.GetValueOrDefault(typeof(T), defaultValue: 0);
    CountWatched.TryAdd(typeof(T), current);
  }

  public static int GetWatchedCount<T>()
  {
    if (!CountWatched.TryGetValue(typeof(T), out int watched))
    {
      Trace.Fail("Ending watcher which hasn't been started.");
      return 0;
    }
    int current = Counter.GetValueOrDefault(typeof(T), defaultValue: 0);
    return current - watched;
  }

  public static int EndWatcher<T>()
  {
    int difference = GetWatchedCount<T>();
    CountWatched.TryRemove(typeof(T), out _);
    return difference;
  }
}