using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace SmashTools;

/// <summary>
/// Helpers for rotating cell offsets within or around a rectangular footprint.
/// </summary>
[PublicAPI]
public static class Ext_Cell
{
  /// <summary>
  /// Rotates <paramref name="cell"/> opposite <paramref name="rot"/>, adjusting for even-sized footprints.
  /// </summary>
  /// <remarks>Diagonal facings rotate 45 degrees from their nearest cardinal facing and round to whole cells.</remarks>
  /// <param name="cell">Offset from the footprint's root cell.</param>
  /// <param name="rot">Facing to rotate against.</param>
  /// <param name="size">Unrotated footprint size.</param>
  /// <returns>The rotated offset.</returns>
  [Pure]
  public static IntVec2 RotatedBy(this IntVec2 cell, Rot8 rot, IntVec2 size)
  {
    switch (rot.AsInt)
    {
      case 0:
        return cell;
      case 1:
        IntVec2 east = new(-cell.z, cell.x);
        return east;
      case 2:
        IntVec2 south = new(-cell.x, -cell.z);
        if (size.x.IsEven())
        {
          south.x++;
        }
        if (size.z.IsEven())
        {
          south.z++;
        }
        return south;
      case 3:
        IntVec2 west = new(cell.z, -cell.x);
        if (size.x.IsEven())
        {
          west.x++;
        }
        if (size.z.IsEven())
        {
          west.z++;
        }
        return west;
      case 4:
      case 5:
      case 6:
      case 7:
        Rot4 cardinal = rot;
        IntVec2 cardinalCell = cell.RotatedBy(cardinal, size);
        Vector2 diagonal = Ext_Math.RotatePointCounterClockwise(
          cardinalCell.x, cardinalCell.z, rot.AsRotationAngle);
        return new IntVec2(Mathf.RoundToInt(diagonal.x),
          Mathf.RoundToInt(diagonal.y));
      default:
        return cell;
    }
  }

  /// <summary>
  /// Rotates <paramref name="cell"/> with <paramref name="rot"/> using the footprint's mirrored facings.
  /// </summary>
  /// <remarks>The root offset (0, 0) remains fixed for every facing.</remarks>
  /// <param name="cell">Offset from the footprint's root cell.</param>
  /// <param name="rot">Facing to rotate with.</param>
  /// <param name="size">Unrotated footprint size.</param>
  /// <returns>The mirrored, rotated offset.</returns>
  [Pure]
  public static IntVec2 MirrorRotatedBy(this IntVec2 cell, Rot4 rot, IntVec2 size)
  {
    if (cell is { x: 0, z: 0 })
      return cell;

    IntVec2 result = cell.RotatedBy(rot, size);
    switch (rot.AsInt)
    {
      case 1:
        result.x *= -1;
        result.z *= -1;
        break;
      case 3:
        if (size.x.IsEven())
        {
          result.z++;
          result.x--;
        }

        if (size.z.IsEven())
        {
          result.z--;
          result.x--;
        }

        result.x *= -1;
        result.z *= -1;
        break;
    }
    return result;
  }
}
