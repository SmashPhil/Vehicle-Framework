using System;
using SmashTools;
using SmashTools.Performance;
using Verse;

namespace Vehicles;

/// <summary>
/// Link between regions for reachability determination
/// </summary>
public class VehicleRegionLink : IPoolable
{
  private const float WeightColorCeiling = 30;

  public VehicleRegion regionA;
  public VehicleRegion regionB;

  public EdgeSpan span;

  public IntVec3 anchor;

  // link cell on opposite region end
  public IntVec3 portal;

  // RegionLink color weights
  private static readonly LinearPool<SimpleColor> ColorWeights = new()
  {
    range = new FloatRange(0, WeightColorCeiling),
    items =
    [
      SimpleColor.White,
      SimpleColor.Green,
      SimpleColor.Yellow,
      SimpleColor.Orange,
      SimpleColor.Red,
      SimpleColor.Magenta
    ]
  };

  public VehicleRegionLink()
  {
#if DEBUG
    ObjectCounter.Increment<VehicleRegionLink>();
#endif
  }

  public bool InPool { get; set; }

  // If only 1 is null, the span should be registered in VehicleRegionLinkDatabase
  // as a half link, and not registered in a region as a full link. It will still be
  // invalid for reachability checks.
  public bool IsValid => regionA != null || regionB != null;

  public IntVec3 Root => span.root;

  public IntVec3 End => SpanEnd(in span);

  public void SetNew(EdgeSpan span)
  {
    Reset();
    this.span = span;
    anchor = VehicleRegionCostCalculator.RegionLinkCenter(this);
  }

  public void Reset()
  {
    regionA = null;
    regionB = null;
  }

  public void Register(VehicleRegion region, Rot4 dir)
  {
    // RegionLinks can double register if the same region is used
    // and the links remained the same
    if (regionA == region || regionB == region)
      return;

    if (regionA is null || !regionA.valid)
    {
      regionA = region;
    }
    else if (regionB is null || !regionB.valid)
    {
      regionB = region;
    }
    // TODO - Add portal set based on in / out link.
    //if (regionA is not null && regionB is not null)
    //{
    //  portal = dir.AsInt switch
    //  {
    //    0 =>,
    //    1 =>,
    //    2 =>,
    //    3 =>,
    //    _ => throw new NotImplementedException(nameof(Rot4)),
    //  };
    //}
  }

  /// <returns>Link is invalid and can be removed from cache</returns>
  public void Deregister(VehicleRegion region)
  {
    if (regionA == region)
    {
      regionA = null;
    }
    else if (regionB == region)
    {
      regionB = null;
    }
  }

  public bool LinksRegions(VehicleRegion regionA, VehicleRegion regionB)
  {
    if (this.regionA == regionA && this.regionB == regionB)
      return true;
    return this.regionA == regionB && this.regionB == regionA;
  }

  /// <summary>
  /// Draws <paramref name="weight"/> on map from this link to <paramref name="regionLink"/>
  /// </summary>
  public void DrawWeight(Map map, VehicleRegionLink regionLink, float weight,
    int duration = 50)
  {
    //Vector3 from = Root.ToVector3();
    //from.y += AltitudeLayer.MapDataOverlay.AltitudeFor();
    //Vector3 to = regionLink.anchor.ToVector3();
    //to.y += AltitudeLayer.MapDataOverlay.AltitudeFor();
    //map.DrawLine_ThreadSafe(from, to, color: WeightColor(weight), duration: duration);
  }

  /// <summary>
  /// Get opposite region linking to <paramref name="region"/>
  /// </summary>
  public VehicleRegion GetOtherRegion(VehicleRegion region)
  {
    return (region != regionA) ? regionA : regionB;
  }

  public VehicleRegion GetInFacingRegion(VehicleRegionLink regionLink)
  {
    if (regionA == regionLink.regionA || regionA == regionLink.regionB)
      return regionA;
    if (regionB == regionLink.regionB || regionB == regionLink.regionA)
      return regionB;
    Log.Warning($"Attempting to fetch region between links {Root} and {regionLink.Root}, " +
      $"but they do not share a region.\n--- Regions ---\n{regionA}\n{regionB}\n" +
      $"{regionLink.regionA}\n{regionLink.regionB}\n");
    return null;
  }

  /// <summary>
  /// Hashcode for cache data
  /// </summary>
  public ulong UniqueHashCode()
  {
    return span.UniqueHashCode();
  }

  /// <summary>
  /// String output with data
  /// </summary>
  public override string ToString()
  {
    return $"({regionA.Id},{regionB.Id}, regions=[spawn={span}, hash={UniqueHashCode()}])";
  }

  public static SimpleColor WeightColor(float weight)
  {
    return ColorWeights.Evaluate(weight);
  }

  private static IntVec3 SpanEnd(in EdgeSpan edgeSpan)
  {
    return edgeSpan.dir switch
    {
      SpanDirection.North => new IntVec3(edgeSpan.root.x, 0, edgeSpan.root.z + edgeSpan.length),
      SpanDirection.East  => new IntVec3(edgeSpan.root.x + edgeSpan.length, 0, edgeSpan.root.z),
      _                   => throw new ArgumentException(nameof(edgeSpan.dir))
    };
  }
}