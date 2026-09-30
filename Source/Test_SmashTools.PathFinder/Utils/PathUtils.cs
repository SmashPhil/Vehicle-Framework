
using CoreLib;
using Unity.Mathematics;

namespace SmashTools.PathFinder.Tests
{
  internal static class PathUtils
  {
    public static Orientation DirectionToCell(int2 c1, int2 c2)
    {
      int xDiff = c1.x - c2.x;
      int zDiff = c1.y - c2.y;
      int xSign = xDiff < 0 ? -1 : xDiff > 0 ? 1 : 0;
      int zSign = zDiff < 0 ? -1 : zDiff > 0 ? 1 : 0;

      int orientation = (xSign, zSign) switch
      {
        (-1, -1) => Orientation.NorthEast,
        (-1, 0) => Orientation.East,
        (-1, 1) => Orientation.SouthEast,
        (0, -1) => Orientation.North,
        (0, 1) => Orientation.South,
        (1, -1) => Orientation.NorthWest,
        (1, 0) => Orientation.West,
        (1, 1) => Orientation.SouthWest,
        _ => Orientation.Invalid
      };
      return new Orientation(orientation);
    }
  }
}
