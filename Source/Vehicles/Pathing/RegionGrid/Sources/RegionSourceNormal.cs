using SmashTools;
using Verse;

namespace Vehicles;

internal sealed class RegionSourceNormal : IRegionSource
{
  RegionType IRegionSource.ExpectedRegionType(IntVec3 cell, VehiclePathingSystem pathingSystem, VehicleDef vehicleDef)
  {
    // Handles map bounds check here as well
    if (!pathingSystem[vehicleDef].VehiclePathGrid.Walkable(cell))
      return RegionType.None;

    if (HugsEdge(cell, pathingSystem, vehicleDef))
      return RegionType.None;

    if (!VerifyCardinalCellSpace(cell, pathingSystem, vehicleDef))
      return RegionType.None;

    return RegionType.Normal;
  }

  private static bool HugsEdge(IntVec3 pos, VehiclePathingSystem pathingSystem, VehicleDef vehicleDef)
  {
    CellRect verticalRect = vehicleDef.VehicleRect(pos, Rot8.North);
    CellRect horizontalRect = vehicleDef.VehicleRect(pos, Rot8.East);

    foreach (IntVec3 cell in new CellRectOverlap(verticalRect, horizontalRect))
    {
      if (!cell.InBounds(pathingSystem.map))
        return true;
    }
    return false;
  }

  private static bool VerifyCardinalCellSpace(IntVec3 cell, VehiclePathingSystem pathingSystem, VehicleDef vehicleDef)
  {
    return vehicleDef.FullRectWalkable(pathingSystem, cell, Rot4.North) ||
           vehicleDef.FullRectWalkable(pathingSystem, cell, Rot4.South) ||
           vehicleDef.FullRectWalkable(pathingSystem, cell, Rot4.East) ||
           vehicleDef.FullRectWalkable(pathingSystem, cell, Rot4.West);
  }
}