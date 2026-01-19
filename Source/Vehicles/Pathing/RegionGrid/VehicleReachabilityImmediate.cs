using System;
using Verse;
using Verse.AI;


namespace Vehicles;

public static class VehicleReachabilityImmediate
{
  /// <summary>
  /// <paramref name="vehicleDef"/> can reach from <paramref name="start"/> to <paramref name="target"/>
  /// </summary>
  public static bool CanReachImmediateVehicle(IntVec3 start, LocalTargetInfo target, Map map, VehicleDef vehicleDef, PathEndMode peMode)
  {
    if (!target.IsValid)
      return false;

    target = (LocalTargetInfo)GenPathVehicles.ResolvePathMode(vehicleDef, map, target.ToTargetInfo(map), ref peMode);
    if (!target.HasThing || target.Thing.def.size.x == 1 && target.Thing.def.size.z == 1)
    {
      if (start == target.Cell) return true;
    }
    else if (start.IsInside(target.Thing))
    {
      return true;
    }
    return peMode == PathEndMode.Touch &&
           TouchPathEndModeUtilityVehicles.IsAdjacentOrInsideAndAllowedToTouch(start, target, map, vehicleDef);
  }

  // TODO 1.6.2144
  /// <summary>
  /// Quick check for <paramref name="vehicle"/> reachability with destination <paramref name="rect"/>
  /// </summary>
  [Obsolete("Use extension method with same name.")]
  public static bool CanReachImmediateVehicle(IntVec3 start, CellRect rect, Map map, PathEndMode peMode, VehiclePawn vehicle)
  {
    IntVec3 c = rect.ClosestCellTo(start);
    return CanReachImmediateVehicle(start, c, map, vehicle.VehicleDef, peMode);
  }

  extension(VehiclePawn vehicle)
  {
    public bool CanReachImmediateVehicle(IntVec3 start, CellRect rect, Map map, PathEndMode peMode)
    {
      IntVec3 c = rect.ClosestCellTo(start);
      return CanReachImmediateVehicle(start, c, map, vehicle.VehicleDef, peMode);
    }

    /// <summary>
    /// Quick check for <paramref name="vehicle"/> reachability
    /// </summary>
    public bool CanReachImmediateVehicle(LocalTargetInfo target, PathEndMode peMode)
    {
      return vehicle.Spawned &&
             CanReachImmediateVehicle(vehicle.Position, target, vehicle.Map, vehicle.VehicleDef, peMode);
    }

    /// <summary>
    /// Quick check for <paramref name="vehicle"/> reachability with non-local constraints
    /// </summary>
    public bool CanReachImmediateNonLocalVehicle(TargetInfo target, PathEndMode peMode)
    {
      return vehicle.Spawned && (target.Map is null || target.Map == vehicle.Map) &&
             vehicle.CanReachImmediateVehicle((LocalTargetInfo)target, peMode);
    }
  }
}