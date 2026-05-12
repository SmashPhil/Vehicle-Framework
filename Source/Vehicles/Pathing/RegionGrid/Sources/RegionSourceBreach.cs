using SmashTools;
using Verse;

namespace Vehicles;

internal sealed class RegionSourceBreach : IRegionSource
{
  RegionType IRegionSource.ExpectedRegionType(IntVec3 cell, IPathingManager manager, VehicleDef vehicleDef)
  {
    VehiclePathGrid pathGrid = manager.GetPathGrid(vehicleDef);
    Map map = manager.Map;

    if (!cell.InBounds(map))
      return RegionType.None;

    if (HugsEdge(map, cell, vehicleDef))
      return RegionType.None;

    if (!CanTraverse(map, pathGrid, cell))
      return RegionType.None;

    return FullRectBreachable(map, pathGrid, vehicleDef, cell) ? RegionType.Normal : RegionType.None;
  }

  private static bool IsBreachable(Thing thing)
  {
    return thing.def.useHitPoints && thing.def.destroyable && thing.Faction is { IsPlayer: true };
  }

  private static bool CanTraverse(Map map, VehiclePathGrid pathGrid, IntVec3 cell)
  {
    if (pathGrid.WalkableFast(cell))
      return true;

    Building edifice = cell.GetEdifice(map);
    return edifice != null && IsBreachable(edifice);
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

  private static bool FullRectBreachable(Map map, VehiclePathGrid pathGrid, VehicleDef vehicleDef, IntVec3 root)
  {
    return RectBreachable(map, pathGrid, vehicleDef, root, Rot4.North) || RectBreachable(map, pathGrid, vehicleDef, root, Rot4.East);

    static bool RectBreachable(Map map, VehiclePathGrid pathGrid, VehicleDef vehicleDef, IntVec3 root, Rot4 rot)
    {
      CellRect cellRect = vehicleDef.VehicleRect(root, rot);
      if (!RectInBounds(map.Size, cellRect))
        return false;

      foreach (IntVec3 cell in cellRect)
      {
        if (!CanTraverse(map, pathGrid, cell))
          return false;
      }
      return true;
    }

    static bool RectInBounds(in IntVec3 bounds, in CellRect cellRect)
    {
      return (uint)cellRect.minX < bounds.x && (uint)cellRect.maxX < bounds.x &&
             (uint)cellRect.minZ < bounds.z && (uint)cellRect.maxZ < bounds.z;
    }
  }
}