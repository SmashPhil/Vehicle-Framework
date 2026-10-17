
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder;

internal static class IndexLookupCache
{
  private static readonly List<LookupEntry> cache = [];

  private static readonly object cacheLock = new();

  internal static int Count
  {
    get
    {
      lock (cacheLock)
      {
        return cache.Count;
      }
    }
  }

  internal static int ReferenceCount(int2 size)
  {
    lock (cacheLock)
    {
      LookupEntry entry = cache.FirstOrDefault(e => e.mapSize.x == size.x && e.mapSize.y == size.y);
      return entry?.refCount ?? 0;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static void Unpack(uint pos, out int x, out int y)
  {
    x = (int)(pos & 0xFFFFu);
    y = (int)(pos >> 16);
  }

  public static NativeArray<uint>.ReadOnly Reserve(int2 size)
  {
    lock (cacheLock)
    {
      LookupEntry entry = cache.FirstOrDefault(e => e.mapSize.x == size.x && e.mapSize.y == size.y);
      if (entry == null)
      {
        entry = new LookupEntry(size);
        cache.Add(entry);
        IndexToCellJob job = new(size, entry.indexToCell);
        job.Run();
      }
      entry.refCount++;
      return entry.indexToCell.AsReadOnly();
    }
  }

  public static void Release(int2 size)
  {
    lock (cacheLock)
    {
      for (int i = 0; i < cache.Count; i++)
      {
        LookupEntry entry = cache[i];
        if (entry.mapSize.x == size.x && entry.mapSize.y == size.y)
        {
          if (--entry.refCount <= 0)
          {
            cache.RemoveAt(i);
            entry.indexToCell.Dispose();
          }
          return;
        }
      }
      Trace.Fail("Releasing claim on index lookup when no references are found.");

    }
  }

  public static void Clear()
  {
    lock (cacheLock)
    {
      foreach (LookupEntry entry in cache)
      {
        entry.Dispose();
      }
      cache.Clear();
    }
  }

  private sealed class LookupEntry : IDisposable
  {
    public readonly int2 mapSize;

    public int refCount;
    public NativeArray<uint> indexToCell;

    public LookupEntry(int2 mapSize)
    {
      this.mapSize = mapSize;
      indexToCell = new NativeArray<uint>(mapSize.x * mapSize.y, Allocator.Persistent);
    }

    public void Dispose()
    {
      if (indexToCell.IsCreated)
      {
        indexToCell.Dispose();
      }
    }
  }
}
