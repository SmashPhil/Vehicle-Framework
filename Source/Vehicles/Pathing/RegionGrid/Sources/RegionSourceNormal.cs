using SmashTools;
using Verse;

namespace Vehicles;

internal sealed class RegionSourceNormal : IRegionSource
{
  RegionType IRegionSource.ExpectedRegionType(IntVec3 cell, IPathingManager manager, VehicleDef vehicleDef)
  {
    // Handles map bounds check here as well
    VehiclePathGrid pathGrid = manager.GetPathGrid(vehicleDef);
    if (!pathGrid.Walkable(cell))
      return RegionType.None;

    if (HugsEdge(manager.Map, cell, vehicleDef))
      return RegionType.None;

    if (!VerifyCardinalCellSpace(manager, cell, vehicleDef))
      return RegionType.None;

    return RegionType.Normal;
  }

  private static bool HugsEdge(Map map, IntVec3 pos, VehicleDef vehicleDef)
  {
    CellRect verticalRect = vehicleDef.VehicleRect(pos, Rot8.North);
    CellRect horizontalRect = vehicleDef.VehicleRect(pos, Rot8.East);

    foreach (IntVec3 cell in new CellRectOverlap(verticalRect, horizontalRect))
    {
      if (!cell.InBounds(map))
        return true;
    }
    return false;
  }

  private static bool VerifyCardinalCellSpace(IPathingManager manager, IntVec3 cell, VehicleDef vehicleDef)
  {
    return manager.FullRectWalkable(vehicleDef, cell, Rot4.North) ||
           manager.FullRectWalkable(vehicleDef, cell, Rot4.South) ||
           manager.FullRectWalkable(vehicleDef, cell, Rot4.East) ||
           manager.FullRectWalkable(vehicleDef, cell, Rot4.West);
  }
}