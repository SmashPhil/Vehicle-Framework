using System.Collections.Generic;
using RimWorld.Planet;
using SmashTools;
using Vehicles.World;
using Verse;

namespace Vehicles;

public static class MapHelper
{
  public static void UnfogMapFromEdge(Map map, VehicleDef vehicleDef = null)
  {
    const int SqrRadius = 30;

    if (!CellFinder.TryFindRandomCellNear(map.Center, map, SqrRadius, Validator, out IntVec3 cell))
    {
      if (!CellFinder.TryFindRandomEdgeCellWith(Validator, map, 0f, out cell))
      {
        if (!CellFinder.TryFindRandomCell(map, Validator, out cell))
          return;
      }
    }
    FloodFillerFog.FloodUnfog(cell, map);
    return;

    bool Validator(IntVec3 cellToCheck)
    {
      if (!cellToCheck.Standable(map))
        return false;
      if (cellToCheck.Roofed(map))
        return false;
      if (vehicleDef != null)
      {
        VehiclePathingSystem mapping = map.GetCachedMapComponent<VehiclePathingSystem>();
        if (mapping[vehicleDef].Suspended)
          mapping.RequestGridsFor(vehicleDef, DeferredGridGeneration.Urgency.Urgent);
        return mapping[vehicleDef].VehicleReachability
         .CanReachMapEdge(cellToCheck, TraverseParms.For(TraverseMode.NoPassClosedDoors));
      }
      return map.reachability.CanReachMapEdge(cellToCheck,
        TraverseParms.For(TraverseMode.NoPassClosedDoorsOrWater));
    }
  }

  /// <summary>
  /// Active skyfallers in a map should prevent the map from being closed
  /// </summary>
  public static bool AnyVehicleSkyfallersBlockingMap(Map map)
  {
    //VehiclePositionManager positionMgr = map.GetDetachedMapComponent<VehiclePositionManager>();
    //foreach (Thing thing in positionMgr.AllSkyfallers)
    //{
    //  if (thing is VehicleSkyfaller)
    //  {
    //    return true;
    //  }
    //}
    return false;
  }

  /// <summary>
  /// Any active aerial vehicles currently providing recon on the map
  /// </summary>
  /// <param name="map"></param>
  public static bool AnyAerialVehiclesInRecon(Map map)
  {
    foreach (AerialVehicleInFlight aerialVehicle in VehicleWorldObjectsHolder.Instance
     .AerialVehicles)
    {
      // Ongoing recon
      if (aerialVehicle.flightPath.InRecon && aerialVehicle.flightPath.Last.Tile == map.Tile)
        return true;
      // Arrival action on tile
      if (aerialVehicle.flightPath.ArrivalAction != null && aerialVehicle.flightPath.Last.Tile == map.Tile)
        return true;
    }
    return false;
  }

  /// <summary>
  /// Vehicle is blocked at <paramref name="cell"/> and will not spawn correctly
  /// </summary>
  public static bool NonStandableOrVehicleBlocked(VehiclePawn vehicle, Map map, IntVec3 cell,
    Rot4 rot)
  {
    return VehicleReservationManager.AnyVehicleInhabitingCells(vehicle.PawnOccupiedCells(cell, rot),
      map) || !vehicle.CellRectStandable(map, cell, rot);
  }

  public static bool ImpassableOrVehicleBlocked(VehiclePawn vehicle, Map map, IntVec3 cell,
    Rot4 rot)
  {
    return VehicleReservationManager.AnyVehicleInhabitingCells(vehicle.PawnOccupiedCells(cell, rot),
      map) || vehicle.LocationRestrictedBySize(map, cell, rot);
  }

  /// <summary>
  /// Vehicle that has reserved <paramref name="cell"/>
  /// </summary>
  /// <param name="vehicle"></param>
  /// <param name="map"></param>
  /// <param name="cell"></param>
  /// <param name="rot"></param>
  public static VehiclePawn VehicleInPosition(VehiclePawn vehicle, Map map, IntVec3 cell, Rot4 rot)
  {
    return VehicleReservationManager.VehicleInhabitingCells(vehicle.PawnOccupiedCells(cell, rot),
      map);
  }

  public static VehicleSkyfaller VehicleSkyfallerInPosition(VehiclePawn vehicle, Map map,
    IntVec3 cell, Rot4 rot)
  {
    IEnumerable<IntVec3> cells = vehicle.PawnOccupiedCells(cell, rot);
    foreach (IntVec3 hitboxCell in cells)
    {
      VehicleSkyfaller vehicleSkyfaller = map.thingGrid.ThingAt<VehicleSkyfaller>(hitboxCell);
      if (vehicleSkyfaller != null)
      {
        return vehicleSkyfaller;
      }
    }
    return null;
  }
}