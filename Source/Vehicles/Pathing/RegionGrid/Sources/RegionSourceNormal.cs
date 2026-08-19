using System.Runtime.CompilerServices;
using UnityEngine;
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

    if (HangsOverEdge(manager.Map, cell, vehicleDef))
      return RegionType.None;

    if (!VerifyCardinalCellSpace(manager, cell, vehicleDef))
      return RegionType.None;

    return RegionType.Normal;
  }

  /// <summary>
  /// Checks cardinal rotations of the vehicle to determine if it will go out of bounds for either rotation.
  /// </summary>
  /// <returns>
  /// <see langword="true"/> if either cardinal rect is out of bounds, <see langword="false"/> otherwise.
  /// </returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool HangsOverEdge(Map map, IntVec3 cell, VehicleDef vehicleDef)
  {
    int padding = vehicleDef.MaxLength;
    return cell.x - padding < 0 || cell.x + padding >= map.Size.x ||
           cell.z - padding < 0 || cell.z + padding >= map.Size.z;
  }

  private static bool VerifyCardinalCellSpace(IPathingManager manager, IntVec3 cell, VehicleDef vehicleDef)
  {
    return manager.FullRectWalkable(vehicleDef, cell, Rot4.North) ||
           manager.FullRectWalkable(vehicleDef, cell, Rot4.South) ||
           manager.FullRectWalkable(vehicleDef, cell, Rot4.East) ||
           manager.FullRectWalkable(vehicleDef, cell, Rot4.West);
  }
}