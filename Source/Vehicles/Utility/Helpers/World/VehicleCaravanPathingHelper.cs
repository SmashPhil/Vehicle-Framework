using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using SmashTools.Performance;
using UnityEngine;
using Verse;

namespace Vehicles.World;

[PublicAPI]
public static class VehicleCaravanPathingHelper
{
  private const int CacheDuration = 100;
  private const int MaxIterations = 10000;

  private static readonly List<TileEstimate> TmpTicksToArrive = [];

  private static int cacheTicks = -1;
  private static VehicleCaravan cachedForCaravan;
  private static int cachedForDest = -1;
  private static int cachedResult = -1;

  /// <summary>
  /// Replaces <see cref="Caravan.NightResting"/> with patch hook in <seealso cref="Patch_CaravanHandling.NoRestForVehicles(Caravan, ref bool)"/>
  /// </summary>
  public static bool ShouldRestAt(VehicleCaravan caravan, in PlanetTile tile)
  {
    const int MaxPushCost = 10000;

    if (!caravan.Spawned)
      return false;
    if (!caravan.needs.AnyPawnsNeedRest)
      return false;
    if (!CaravanNightRestUtility.RestingNowAt(caravan.Tile))
      return false;
    // TODO VF-202: Allow passengers in fully autonomous vehicle caravans to rest 24/7
    if (!ShouldRestAt(caravan.VehiclesListForReading, tile))
      return false;

    return !caravan.vehiclePather.Moving ||
      !Caravan_PathFollower.IsValidFinalPushDestination(caravan.vehiclePather.Destination) ||
      caravan.vehiclePather.NextTile != caravan.vehiclePather.Destination ||
      Mathf.CeilToInt(caravan.vehiclePather.nextTileCostLeft) > MaxPushCost;
  }

  public static bool ShouldRestAt(List<VehiclePawn> vehicles, in PlanetTile tile)
  {
    bool fullyAutonomous = true;
    foreach (VehiclePawn vehicle in vehicles)
    {
      fullyAutonomous &= (vehicle.MovementPermissions & VehiclePermissions.Autonomous) != 0;
      // TODO VF-201: Driving at night feature needs to be reworked to allow for others to rest inside the vehicle and/or take turns.
      //NavigationCategory navigationCategory = SettingsCache.TryGetValue(vehicle.VehicleDef,
      //	typeof(VehicleDef), nameof(VehicleDef.navigationCategory),
      //	vehicle.VehicleDef.navigationCategory);
      //if (navigationCategory == NavigationCategory.Automatic)
      //	return false;
    }
    return !fullyAutonomous && CaravanNightRestUtility.RestingNowAt(tile);
  }

  public static bool ShouldRestAt(List<VehicleDef> vehicleDefs, in PlanetTile tile)
  {
    bool fullyAutonomous = true;
    foreach (VehicleDef vehicleDef in vehicleDefs)
    {
      fullyAutonomous &= (vehicleDef.MovementPermissions & VehiclePermissions.Autonomous) != 0;

      // TODO VF-201: Driving at night feature needs to be reworked to allow for others to rest inside the vehicle and/or take turns.
      //NavigationCategory navigationCategory = SettingsCache.TryGetValue(vehicleDef,
      //	typeof(VehicleDef), nameof(VehicleDef.navigationCategory), vehicleDef.navigationCategory);
      //if (navigationCategory == NavigationCategory.Automatic)
      //	return false;
    }
    return !fullyAutonomous && CaravanNightRestUtility.RestingNowAt(tile);
  }

  public static int EstimatedTicksToArrive([NotNull] VehicleCaravan caravan, bool allowCaching)
  {
    if (allowCaching && caravan == cachedForCaravan &&
      caravan.vehiclePather.Destination == cachedForDest &&
      Find.TickManager.TicksGame - cacheTicks < CacheDuration)
    {
      return cachedResult;
    }

    PlanetTile to = PlanetTile.Invalid;
    int result = 0;
    if (caravan.Spawned && caravan.vehiclePather.Moving && caravan.vehiclePather.curPath != null)
    {
      to = caravan.vehiclePather.Destination;
      List<VehicleDef> vehicleDefs =
        caravan.VehiclesListForReading.Select(vehicle => vehicle.VehicleDef).ToList();
      result = EstimatedTicksToArrive(vehicleDefs, caravan.Tile, to, caravan.vehiclePather.curPath,
        caravan.vehiclePather.nextTileCostLeft, caravan.TicksPerMove, Find.TickManager.TicksAbs);
    }
    if (allowCaching)
    {
      cacheTicks = Find.TickManager.TicksGame;
      cachedForCaravan = caravan;
      cachedForDest = to;
      cachedResult = result;
    }
    return result;
  }

  public static int EstimatedTicksToArrive([NotNull] VehicleCaravan caravan, in PlanetTile from, in PlanetTile to)
  {
    using WorldPath worldPath = Find.World.GetComponent<WorldVehiclePathfinder>().FindPath(from, to, caravan);
    if (!worldPath.Found)
      return 0;

    int result = EstimatedTicksToArrive(caravan.VehiclesListForReading.UniqueVehicleDefsInList(), from, to, worldPath,
      nextTileCostLeft: 0f, caravan.TicksPerMove, Find.TickManager.TicksAbs);
    return result;
  }

  public static int EstimatedTicksToArrive(List<VehicleDef> vehicleDefs, in PlanetTile from, in PlanetTile to,
    WorldPath path, float nextTileCostLeft, int caravanTicksPerMove, int curTicksAbs)
  {
    using var cs = GlobalObjectPool.Get(out List<TileEstimate> estimates);
    EstimatedTicksToArriveToEvery(vehicleDefs, from, to, path, nextTileCostLeft,
      caravanTicksPerMove, curTicksAbs, estimates);
    return EstimatedTicksToArrive(to, estimates);
  }

  private static void EstimatedTicksToArriveToEvery(List<VehicleDef> vehicleDefs, in PlanetTile from, in PlanetTile to,
    WorldPath path, float nextTileCostLeft, int caravanTicksPerMove, int curTicksAbs,
    List<TileEstimate> outTicksToArrive)
  {
    outTicksToArrive.Clear();
    outTicksToArrive.Add(new TileEstimate(from, 0));
    if (from == to)
    {
      outTicksToArrive.Add(new TileEstimate(to, 0));
      return;
    }
    const int RestDuration = GenDate.TicksPerDay / 3 - 1;
    const int MovementDuration = GenDate.TicksPerDay - RestDuration;

    int result = 0;
    PlanetTile curTile = from;
    int pathSteps = 0;
    int ticksToMove = 0;
    int nonRestTicks;
    if (ShouldRestAt(vehicleDefs, from) && CaravanNightRestUtility.WouldBeRestingAt(from, curTicksAbs))
    {
      if (VehicleCaravan_PathFollower.IsValidFinalPushDestination(to) && (path.Peek(0) == to ||
        (nextTileCostLeft <= 0f && path.NodesLeftCount >= 2 && path.Peek(1) == to)))
      {
        TicksPerMoveData data = new() { ticksPerMove = caravanTicksPerMove, tile = from, nextTile = to, };
        int costToMove = path.Peek(0) == to ?
          Mathf.CeilToInt(nextTileCostLeft) :
          VehicleCaravan_PathFollower.CostToMove(vehicleDefs, in data);
        if (costToMove <= GenDate.TicksPerDay / 6)
        {
          result += costToMove;
          outTicksToArrive.Add(new TileEstimate(to, result + costToMove));
          return;
        }
      }
      result += CaravanNightRestUtility.LeftRestTicksAt(from, curTicksAbs);
      nonRestTicks = MovementDuration;
    }
    else
    {
      nonRestTicks = CaravanNightRestUtility.LeftNonRestTicksAt(from, curTicksAbs);
    }
    for (int i = 0; i < MaxIterations; i++)
    {
      if (ticksToMove <= 0)
      {
        if (curTile == to)
        {
          outTicksToArrive.Add(new TileEstimate(to, result));
          return;
        }
        bool firstInPath = pathSteps == 0;
        PlanetTile prevTile = curTile;
        curTile = path.Peek(pathSteps++);
        outTicksToArrive.Add(new TileEstimate(prevTile, result));
        TicksPerMoveData data = new()
        {
          ticksPerMove = caravanTicksPerMove,
          tile = prevTile,
          nextTile = curTile,
          ticksAbs = curTicksAbs + result
        };
        ticksToMove = firstInPath ?
          Mathf.CeilToInt(nextTileCostLeft) :
          VehicleCaravan_PathFollower.CostToMove(vehicleDefs, in data);
      }
      if (nonRestTicks < ticksToMove)
      {
        result += nonRestTicks;
        ticksToMove -= nonRestTicks;
        if (curTile == to && ticksToMove <= 10000 &&
          Caravan_PathFollower.IsValidFinalPushDestination(to))
        {
          result += ticksToMove;
          outTicksToArrive.Add(new TileEstimate(to, result));
          return;
        }
        result += RestDuration;
        nonRestTicks = MovementDuration;
      }
      else
      {
        result += ticksToMove;
        nonRestTicks -= ticksToMove;
        ticksToMove = 0;
      }
    }
    Log.ErrorOnce("Could not calculate estimated ticks to arrive. Too many iterations.", 1837451324);
    outTicksToArrive.Add(new TileEstimate(to, result));
  }

  private static int EstimatedTicksToArrive(PlanetTile destinationTile,
    List<TileEstimate> estimatedTicksToArriveToEvery)
  {
    if (!destinationTile.Valid)
      return 0;

    foreach (TileEstimate estimate in estimatedTicksToArriveToEvery)
    {
      if (destinationTile == estimate.tile)
        return estimate.ticksToArrive;
    }
    return 0;
  }

  private readonly struct TileEstimate(PlanetTile tile, int ticksToArrive)
  {
    public readonly PlanetTile tile = tile;
    public readonly int ticksToArrive = ticksToArrive;
  }
}