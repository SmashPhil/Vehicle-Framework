using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Key = System.ValueTuple<int, int>;

namespace CoreLib.Performance;

[PublicAPI]
public sealed class MapObjectPool<T>
{
  private static readonly Dictionary<Key, Entry> PoolOfPools = new();

  public ObjectPool<T> Get(int width, int height, int poolSize, Func<T> factory = null)
  {
    Key key = new(width, height);
    if (!PoolOfPools.TryGetValue(key, out Entry entry))
    {
      entry = new Entry(poolSize, factory);
      PoolOfPools[key] = entry;
    }
    entry.RefCount++;
    return entry.pool;
  }

  public void Release(int width, int height)
  {
    Key key = new(width, height);
    if (!PoolOfPools.TryGetValue(key, out Entry entry))
    {
      Logger.Error($"No object pool with size ({width},{height}) found in MapObjectPool");
      return;
    }
    entry.RefCount--;
    if (entry.RefCount == 0)
    {
      entry.Dispose();
      PoolOfPools.Remove(key);
    }
  }

  private record Entry : IDisposable
  {
    public readonly ObjectPool<T> pool;

    public Entry(int size, Func<T> factory = null)
    {
      pool = factory == null ? 
        new ObjectPool<T>(size) : 
        new ObjectPool<T>(size, factory);
    }

    public int RefCount { get; set; }

    public void Dispose()
    {
      // If T is IDisposable, then the items need to be disposed from Clear
      pool.Clear();
    }
  }
}
