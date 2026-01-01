using Verse;

namespace Vehicles;

internal sealed class RegionSourceNormal : IRegionSource
{
  RegionType IRegionSource.ExpectedRegionType(IntVec3 cell, VehiclePathingSystem pathingSystem, VehicleDef vehicleDef)
  {
    // Handles map bounds check here as well
    if (!pathingSystem[vehicleDef].VehiclePathGrid.Walkable(cell))
      return RegionType.None;

    if (!VerifyCardinalCellSpace(cell, pathingSystem, vehicleDef))
      return RegionType.None;

    return RegionType.Normal;
  }

  private static bool VerifyCardinalCellSpace(IntVec3 cell, VehiclePathingSystem mapping, VehicleDef vehicleDef)
  {
    return vehicleDef.FullRectWalkable(mapping, cell, Rot4.North) ||
           vehicleDef.FullRectWalkable(mapping, cell, Rot4.South) ||
           vehicleDef.FullRectWalkable(mapping, cell, Rot4.East) ||
           vehicleDef.FullRectWalkable(mapping, cell, Rot4.West);
  }
}