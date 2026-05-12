using JetBrains.Annotations;
using SmashTools;
using Verse;

namespace Vehicles;

/// <summary>
/// Region and room retrieval helper methods
/// </summary>
[PublicAPI]
public static class VehicleRegionAndRoomQuery
{
  public static VehicleRegion RegionAt(IntVec3 cell, Map map, VehicleDef vehicleDef,
    RegionType allowedRegionTypes = RegionType.Set_Passable)
  {
    return RegionAt(cell, map.GetCachedMapComponent<VehiclePathingSystem>(), vehicleDef, allowedRegionTypes);
  }

  public static VehicleRegion RegionAt(IntVec3 cell, VehiclePathingSystem mapping, VehicleDef vehicleDef,
    RegionType allowedRegionTypes = RegionType.Set_Passable)
  {
    return RegionAt(cell, mapping, vehicleDef, RegionGridType.Normal, allowedRegionTypes);
  }

  /// <summary>
  /// Retrieve region at <paramref name="cell"/> for <paramref name="vehicleDef"/>
  /// </summary>
  public static VehicleRegion RegionAt(IntVec3 cell, VehiclePathingSystem mapping, VehicleDef vehicleDef,
    RegionGridType gridType, RegionType allowedRegionTypes = RegionType.Set_Passable)
  {
    if (!cell.InBounds(mapping.map))
    {
      return null;
    }
    VehicleRegion validRegionAt = mapping[vehicleDef].VehicleRegionGridManager[gridType].GetValidRegionAt(cell);
    if (validRegionAt != null && (allowedRegionTypes & validRegionAt.type) == validRegionAt.type)
    {
      return validRegionAt;
    }
    return null;
  }

  public static VehicleRegion RegionAt(IntVec3 cell, IPathingManager pathing, VehicleDef vehicleDef,
    RegionType allowedRegionTypes = RegionType.Set_Passable)
  {
    return RegionAt(cell, pathing, vehicleDef, RegionGridType.Normal, allowedRegionTypes);
  }

  /// <summary>
  /// Retrieve region at <paramref name="cell"/> for <paramref name="vehicleDef"/>
  /// </summary>
  public static VehicleRegion RegionAt(IntVec3 cell, IPathingManager pathing, VehicleDef vehicleDef,
    RegionGridType gridType, RegionType allowedRegionTypes = RegionType.Set_Passable)
  {
    if (!cell.InBounds(pathing.Map))
    {
      return null;
    }
    VehicleRegionGridManager gridManager = pathing.GetRegionGridManager(vehicleDef);
    VehicleRegion validRegionAt = gridManager[gridType].GetValidRegionAt(cell);
    if (validRegionAt != null && (allowedRegionTypes & validRegionAt.type) == validRegionAt.type)
    {
      return validRegionAt;
    }
    return null;
  }

  /// <summary>
  /// Get region at <paramref name="thing"/>'s position for <paramref name="vehicleDef"/>
  /// </summary>
  public static VehicleRegion GetRegion(this Thing thing, VehicleDef vehicleDef,
    RegionType allowedRegionTypes = RegionType.Set_Passable)
  {
    if (!thing.Spawned)
    {
      return null;
    }
    return !thing.Spawned ? null : RegionAt(thing.Position, thing.Map, vehicleDef, allowedRegionTypes);
  }

  /// <summary>
  /// Get room at <paramref name="cell"/> for <paramref name="vehicleDef"/>
  /// </summary>
  public static VehicleRoom RoomAt(IntVec3 cell, Map map, VehicleDef vehicleDef,
    RegionType allowedRegionTypes = RegionType.Set_Passable)
  {
    VehicleRegion region = RegionAt(cell, map, vehicleDef, allowedRegionTypes);
    return region?.Room;
  }

  public static VehicleRoom RoomAtFast(IntVec3 cell, Map map, VehicleDef vehicleDef,
    RegionType allowedRegionTypes = RegionType.Set_Passable)
  {
    return RoomAtFast(cell, map, vehicleDef, RegionGridType.Normal, allowedRegionTypes);
  }

  /// <summary>
  /// Quick retrieval of room at <paramref name="cell"/> for <paramref name="vehicleDef"/>
  /// </summary>
  public static VehicleRoom RoomAtFast(IntVec3 cell, Map map, VehicleDef vehicleDef, RegionGridType gridType,
    RegionType allowedRegionTypes = RegionType.Set_Passable)
  {
    VehiclePathingSystem pathingSystem = map.GetCachedMapComponent<VehiclePathingSystem>();
    VehicleRegion validRegionAt = pathingSystem[vehicleDef].VehicleRegionGridManager[gridType].GetValidRegionAt(cell);
    if (validRegionAt != null && (validRegionAt.type & allowedRegionTypes) != RegionType.None)
    {
      return validRegionAt.Room;
    }
    return null;
  }
}