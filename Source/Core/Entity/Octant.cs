using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace CoreLib;

[PublicAPI]
public readonly struct Octant(int value)
{
  // Orientation ints in octant format
  public const int North = 0;
  public const int NorthEast = 1;
  public const int East = 2;
  public const int SouthEast = 3;
  public const int South = 4;
  public const int SouthWest = 5;
  public const int West = 6;
  public const int NorthWest = 7;

  public int AsInt => value;

  public bool IsValid => value is >= North and <= NorthWest;

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool IsCardinal(int value)
  {
    return (value & 1) == 0;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool IsDiagonal(int value)
  {
    return !IsCardinal(value);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool IsHorizontal(int value)
  {
    return (value & 3) == 2;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool IsVertical(int value)
  {
    return (value & 3) == 0;
  }
}