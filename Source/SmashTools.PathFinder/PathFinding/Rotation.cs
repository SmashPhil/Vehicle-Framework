using System.Runtime.CompilerServices;
using CoreLib;
using JetBrains.Annotations;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  public struct Rotation
  {
    private const int CardinalCutoff = 4;

    // Rotation ints in octant format
    public const int North = Octant.North;
    public const int East = Octant.East;
    public const int South = Octant.South;
    public const int West = Octant.West;
    public const int NorthEast = 1;
    public const int SouthEast = 3;
    public const int SouthWest = 5;
    public const int NorthWest = 7;
    public const int Invalid = 0xF;

    internal static readonly int4 CardinalHeadings =
      new(Octant.North, Octant.East, Octant.South, Octant.West);
    internal static readonly int4 DiagonalHeadings =
      new(Octant.NorthEast, Octant.SouthEast, Octant.SouthWest, Octant.NorthWest);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToOctant(int rot)
    {
      return rot < CardinalCutoff ? rot << 1 : ((rot - CardinalCutoff) << 1) + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Heading(int x, int y, int nx, int ny)
    {
      // Pack into nibbles for quick bitwise lookup.
      // NW (7) N (0)  NE (1)
      // W  (6) - (15) E  (2)
      // SW (5) S (4)  SE (3)
      const ulong Encoding = 4415513413UL;

      int dx = nx - x;
      int dy = ny - y;
      int decode = (dx + 1) + 3 * (dy + 1);
      return (int)((Encoding >> (decode * 4)) & 0xFUL);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int4 TurnCost(int h1, int4 h2)
    {
      int4 cw = (h2 - h1) & 7;
      int4 ccw = -cw & 7;
      return math.min(cw, ccw);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int TurnCost(int h1, int h2)
    {
      int cw = (h2 - h1) & 7;
      int ccw = -cw & 7;
      return math.min(cw, ccw);
    }
  }
}