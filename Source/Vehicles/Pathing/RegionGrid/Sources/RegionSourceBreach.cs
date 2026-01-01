using Verse;
using VehiclePathData = Vehicles.VehiclePathingSystem.VehiclePathData;

namespace Vehicles;

internal sealed class RegionSourceBreach : IRegionSource
{
  RegionType IRegionSource.ExpectedRegionType(IntVec3 cell, VehiclePathingSystem pathingSystem, VehicleDef vehicleDef)
  {
    VehiclePathData pathData = pathingSystem[vehicleDef];
    Map map = pathingSystem.map;
    if (!cell.InBounds(map))
      return RegionType.None;
    if (!CanTraverse(map, pathData, cell))
      return RegionType.None;

    return FullRectBreachable(map, vehicleDef, pathData, cell) ? RegionType.Normal : RegionType.None;
  }

  private static bool IsBreachable(Thing thing)
  {
    return thing.def.useHitPoints && thing.def.destroyable;
  }

  private static bool CanTraverse(Map map, VehiclePathData pathData, IntVec3 cell)
  {
    if (pathData.VehiclePathGrid.WalkableFast(cell))
      return true;

    Building edifice = cell.GetEdifice(map);
    return edifice != null && IsBreachable(edifice);
  }

  private static bool FullRectBreachable(Map map, VehicleDef vehicleDef, VehiclePathData pathData, IntVec3 root)
  {
    return RectBreachable(map, vehicleDef, pathData, root, Rot4.North) || RectBreachable(map, vehicleDef, pathData, root, Rot4.East);

    static bool RectBreachable(Map map, VehicleDef vehicleDef, VehiclePathData pathData, IntVec3 root, Rot4 rot)
    {
      CellRect cellRect = vehicleDef.VehicleRect(root, rot);
      if (!RectInBounds(map.Size, cellRect))
        return false;

      foreach (IntVec3 cell in cellRect)
      {
        if (!CanTraverse(map, pathData, cell))
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