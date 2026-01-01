using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using CoreLib.Performance;
using Vehicles.Config;
using Verse;

namespace Vehicles;

internal sealed class VehicleRegionLinkDatabase
{
  // Active links
  private readonly ConcurrentDictionary<ulong, VehicleRegionLink> normal;
  private readonly ConcurrentDictionary<ulong, VehicleRegionLink> breach;

  private readonly ObjectPool<VehicleRegionLink> linkPool;

  public VehicleRegionLinkDatabase(int linkPoolSize)
  {
    normal = [];
    if (FeatureFlags.RaidersEnabled)
    {
      breach = [];
    }
    linkPool = new ObjectPool<VehicleRegionLink>(linkPoolSize);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  internal ConcurrentDictionary<ulong, VehicleRegionLink> GetActiveLinks(RegionGridType gridType)
  {
    return gridType switch
    {
      RegionGridType.Normal => normal,
      RegionGridType.Breach => breach,
      _ => throw new NotImplementedException(gridType.ToString())
    };
  }

  public void Return(VehicleRegionLink regionLink)
  {
    GetActiveLinks(regionLink.gridType).TryRemove(regionLink.UniqueHashCode(), out _);
    linkPool.Return(regionLink);
  }

  /// <summary>
  /// Region link between <paramref name="span"/>
  /// </summary>
  public VehicleRegionLink LinkFrom(EdgeSpan span, RegionGridType gridType)
  {
    ulong key = span.UniqueHashCode();
    var activeLinks = GetActiveLinks(gridType);
    if (!activeLinks.TryGetValue(key, out VehicleRegionLink regionLink))
    {
      regionLink = linkPool.Get();
      regionLink.SetNew(span, gridType);
      activeLinks.TryAdd(key, regionLink);
    }
    return regionLink;
  }
}
