using System.Collections.Concurrent;
using System.Collections.Generic;
using CoreLib;
using RimWorld;
using Verse;

namespace Vehicles;

/// <summary>
/// Cache results from reachability calculations for faster retrieval
/// </summary>
public class VehicleReachabilityCache
{
  private readonly ConcurrentDictionary<CachedEntry, bool> cacheDict = [];

  private readonly HashSet<CachedEntry> cachedEntries = [];

  /// <summary>
  /// Cache count
  /// </summary>
  public int Count
  {
    get
    {
      return cacheDict.Count;
    }
  }

  /// <summary>
  /// Clear reachability cache
  /// </summary>
  public void Clear()
  {
    cacheDict.Clear();
  }

  /// <summary>
  /// Retrieve cached result for reachability from <paramref name="from"/> to <paramref name="to"/>
  /// </summary>
  public BoolUnknown CachedResultFor(VehicleRoom from, VehicleRoom to, TraverseParms traverseParms)
  {
    if (cacheDict.TryGetValue(new CachedEntry(from.Id, to.Id, traverseParms), out bool reachable))
    {
      return reachable ? BoolUnknown.True : BoolUnknown.False;
    }
    return BoolUnknown.Unknown;
  }

  /// <summary>
  /// Add cached result for reachability from <paramref name="from"/> to <paramref name="to"/>
  /// </summary>
  public void AddCachedResult(VehicleRoom from, VehicleRoom to, TraverseParms traverseParams, bool reachable)
  {
    CachedEntry key = new(from.Id, to.Id, traverseParams);
    cacheDict.TryAdd(key, reachable);
  }

  /// <summary>
  /// Clear all results for <paramref name="vehicle"/>
  /// </summary>
  /// <param name="vehicle"></param>
  public void ClearFor(VehiclePawn vehicle)
  {
    using ClearOnDispose<CachedEntry> cod = new(cachedEntries);
    foreach ((CachedEntry entry, _) in cacheDict)
    {
      if (entry.traverseParms.pawn == vehicle)
      {
        cachedEntries.Add(entry);
      }
    }
    foreach (CachedEntry cachedEntry in cachedEntries)
    {
      cacheDict.TryRemove(cachedEntry, out _);
    }
  }

  /// <summary>
  /// Clear all results containing results targeting <paramref name="hostileTo"/>
  /// </summary>
  /// <param name="hostileTo"></param>
  public void ClearForHostile(Thing hostileTo)
  {
    using ClearOnDispose<CachedEntry> cod = new(cachedEntries);
    foreach ((CachedEntry entry, _) in cacheDict)
    {
      if (entry.traverseParms.pawn is {} pawn && pawn.HostileTo(hostileTo))
      {
        cachedEntries.Add(entry);
      }
    }
    foreach (CachedEntry cachedEntry in cachedEntries)
    {
      cacheDict.TryRemove(cachedEntry, out _);
    }
  }

  /// <summary>
  /// Cached result data for reachability between two <see cref="VehicleRegion"/>
  /// </summary>
  private readonly record struct CachedEntry
  {
    private readonly int from;
    private readonly int to;
    public readonly TraverseParms traverseParms;

    public CachedEntry(int from, int to, TraverseParms traverseParms)
    {
      this = default;
      if (from < to)
      {
        this.from = from;
        this.to = to;
      }
      else
      {
        this.from = to;
        this.to = from;
      }
      this.traverseParms = traverseParms;
    }

    public override int GetHashCode()
    {
      int seed = Gen.HashCombineInt(from, to);
      return Gen.HashCombineStruct(seed, traverseParms);
    }
  }
}