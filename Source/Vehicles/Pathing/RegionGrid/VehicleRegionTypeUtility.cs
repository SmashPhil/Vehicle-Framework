using System;
using Verse;

namespace Vehicles;

/// <summary>
/// RegionType helper methods specific to VehicleDefs
/// </summary>
public static class VehicleRegionTypeUtility
{
  /// <summary>
  /// Getter for expected region type at <paramref name="cell"/> for <paramref name="vehicleDef"/>
  /// </summary>
  [Obsolete($"Expected region type is now provided by IRegionSource.", error: true)]
  public static RegionType GetExpectedRegionType(IntVec3 cell, VehiclePathingSystem mapping, VehicleDef vehicleDef)
  {
    // Handles map bounds check here as well
    if (!mapping[vehicleDef].VehiclePathGrid.Walkable(cell))
      return RegionType.None;

    if (!VerifyCardinalCellSpace(cell, mapping, vehicleDef))
      return RegionType.None;

    return RegionType.Normal;
  }

  // TODO - Account for non-uniform combinations (eg. Y shape)
  /// <summary>
  /// Verify if non-uniform rotations still allow for movement on this cell
  /// </summary>
  private static bool VerifyCardinalCellSpace(IntVec3 cell, VehiclePathingSystem mapping, VehicleDef vehicleDef)
  {
    return vehicleDef.FullRectWalkable(mapping, cell, Rot4.North) ||
      vehicleDef.FullRectWalkable(mapping, cell, Rot4.South) ||
      vehicleDef.FullRectWalkable(mapping, cell, Rot4.East) ||
      vehicleDef.FullRectWalkable(mapping, cell, Rot4.West);
  }
}