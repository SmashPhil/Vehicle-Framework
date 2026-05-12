using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vehicles;

/// <summary>
/// Region cost calculator inner data
/// </summary>
public class VehicleRegionCostCalculatorWrapper
{
  private readonly IPathingManager manager;
  private readonly Map map;
  private IntVec3 endCell;

  private VehicleDef vehicleDef;

  private float moveTicksCardinal;
  private float moveTicksDiagonal;

  private readonly VehicleRegionCostCalculator vehicleRegionCostCalculator;
  private VehicleRegion cachedRegion;
  private VehicleRegionLink cachedBestLink;
  private VehicleRegionLink cachedSecondBestLink;

  private readonly HashSet<VehicleRegion> destRegions = [];

  private int cachedBestLinkCost;
  private int cachedSecondBestLinkCost;
  private bool cachedRegionIsDestination;

  // TODO 1.7 - Remove. Even though it's implicitly convertible to IPathingManager, this is an API breaking change.
  public VehicleRegionCostCalculatorWrapper(VehiclePathingSystem mapping, VehicleDef vehicleDef) :
    this((IPathingManager)mapping, vehicleDef)
  {
  }

  public VehicleRegionCostCalculatorWrapper(IPathingManager manager, VehicleDef vehicleDef)
  {
    this.manager = manager;
    this.vehicleDef = vehicleDef;
    map = manager.Map;
    vehicleRegionCostCalculator = new VehicleRegionCostCalculator(manager, vehicleDef);
  }

  /// <summary>
  /// Initialize cost calculator for region link traversal
  /// </summary>
  internal void Init(CellRect end, TraverseParms traverseParms, float moveTicksCardinal,
    float moveTicksDiagonal, AvoidGrid avoidGrid, bool drafted, List<int> disallowedCorners)
  {
    VehiclePawn vehicle = traverseParms.pawn as VehiclePawn;
    VehicleDef vehicleDef = vehicle!.VehicleDef;

    this.moveTicksCardinal = moveTicksCardinal;
    this.moveTicksDiagonal = moveTicksDiagonal;
    endCell = end.CenterCell;
    cachedRegion = null;
    cachedBestLink = null;
    cachedSecondBestLink = null;
    cachedBestLinkCost = 0;
    cachedSecondBestLinkCost = 0;
    cachedRegionIsDestination = false;
    destRegions.Clear();
    if (end is { Width: 1, Height: 1 })
    {
      VehicleRegion region =
        VehicleRegionAndRoomQuery.RegionAt(endCell, manager, vehicleDef);
      if (region != null)
      {
        destRegions.Add(region);
      }
    }
    else
    {
      foreach (IntVec3 intVec in end)
      {
        if (intVec.InBounds(map) &&
          !disallowedCorners.Contains(map.cellIndices.CellToIndex(intVec)))
        {
          VehicleRegion region2 = VehicleRegionAndRoomQuery.RegionAt(intVec, manager, vehicleDef);
          if (region2 != null)
          {
            if (region2.Allows(traverseParms))
            {
              destRegions.Add(region2);
            }
          }
        }
      }
    }
    if (destRegions.Count == 0)
    {
      Log.Error(
        "Couldn't find any destination regions. This shouldn't ever happen because we've checked reachability.");
    }
    vehicleRegionCostCalculator.Init(end, destRegions, traverseParms, moveTicksCardinal,
      moveTicksDiagonal, avoidGrid, drafted);
  }

  /// <summary>
  /// Calculate approximate total path cost through regions from <paramref name="cellIndex"/> to <see cref="endCell"/>
  /// </summary>
  public int GetPathCostFromDestToRegion(int cellIndex, in TraverseParms parms)
  {
    VehiclePawn vehicle = parms.pawn as VehiclePawn;
    VehicleDef vehicleDef = vehicle!.VehicleDef;
    VehicleRegion region = manager.GetRegionGridManager(vehicleDef)[VehicleRegionGridManager.GetGridType(parms)]
      .DirectGrid[cellIndex];
    IntVec3 cell = map.cellIndices.IndexToCell(cellIndex);
    if (region != cachedRegion)
    {
      cachedRegionIsDestination = destRegions.Contains(region);
      if (cachedRegionIsDestination)
      {
        return OctileDistanceToEnd(cell);
      }
      cachedBestLinkCost = vehicleRegionCostCalculator.GetRegionBestDistances(region,
        out cachedBestLink, out cachedSecondBestLink, out cachedSecondBestLinkCost);
      cachedRegion = region;
    }
    else if (cachedRegionIsDestination)
    {
      return OctileDistanceToEnd(cell);
    }
    if (cachedBestLink != null)
    {
      int num = vehicleRegionCostCalculator.RegionLinkDistance(cell, cachedBestLink, 1);
      if (cachedSecondBestLink != null)
      {
        int num2 = vehicleRegionCostCalculator.RegionLinkDistance(cell, cachedSecondBestLink, 1);
        return Mathf.Min(cachedSecondBestLinkCost + num2, cachedBestLinkCost + num) +
          OctileDistanceToEndEps(cell);
      }
      return cachedBestLinkCost + num + OctileDistanceToEndEps(cell);
    }
    return VehiclePathGrid.ImpassableCost;
  }

  /// <summary>
  /// Octile distance from <paramref name="cell"/> to <see cref="endCell"/>
  /// </summary>
  /// <param name="cell"></param>
  /// <returns></returns>
  private int OctileDistanceToEnd(IntVec3 cell)
  {
    int dx = Mathf.Abs(cell.x - endCell.x);
    int dz = Mathf.Abs(cell.z - endCell.z);
    return GenMath.OctileDistance(dx, dz, Mathf.RoundToInt(moveTicksCardinal),
      Mathf.RoundToInt(moveTicksDiagonal));
  }

  /// <summary>
  /// Octile distance from <paramref name="cell"/> to <see cref="endCell"/> estimate
  /// </summary>
  /// <param name="cell"></param>
  private int OctileDistanceToEndEps(IntVec3 cell)
  {
    int dx = Mathf.Abs(cell.x - endCell.x);
    int dz = Mathf.Abs(cell.z - endCell.z);
    return GenMath.OctileDistance(dx, dz, 2, 3);
  }
}