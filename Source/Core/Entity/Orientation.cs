using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace CoreLib;

[PublicAPI]
public readonly struct Orientation(int value)
{
  // Orientation ints in clockwise format
  public const int North = 0;
  public const int East = 1;
  public const int South = 2;
  public const int West = 3;
  public const int NorthEast = 4;
  public const int SouthEast = 5;
  public const int SouthWest = 6;
  public const int NorthWest = 7;

  // Invalid int is 200 to align with Verse.Rot4
  public const int Invalid = 200;

  private const int CardinalCutoff = 4;

  public bool IsValid => value is >= 0 and < 8;

  public int AsInt => value;

  public bool IsCardinal => value is >= North and < NorthEast;

  public bool IsDiagonal => value is >= NorthEast and < NorthWest;

  public bool IsHorizontal => IsCardinal && (value & 1) != 0;

  public bool IsVertical => IsCardinal && (value & 1) == 0;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int ToOctant(int rot)
  {
    return rot < CardinalCutoff ? rot << 1 : ((rot - CardinalCutoff) << 1) + 1;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Octant ToOctant(Orientation rot)
  {
    int value = rot.AsInt;
    return new Octant(value < CardinalCutoff ? value << 1 : ((value - CardinalCutoff) << 1) + 1);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static Orientation FromOctant(Octant rot)
  {
    int value = rot.AsInt;
    return new Orientation((value & 1) == 0 ? value >> 1 : (value >> 1) + 4);
  }

  public override string ToString()
  {
    return value switch
    {
      0 => "north",
      1 => "east",
      2 => "south",
      3 => "west",
      4 => "northeast",
      5 => "southeast",
      6 => "southwest",
      7 => "northwest",
      _ => "invalid"
    };
  }
}
