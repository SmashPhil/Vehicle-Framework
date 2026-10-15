using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace SmashTools;

public static class Ext_Numeric
{
  /// <summary>
  /// Extension method for <see cref="Mathf.Clamp(float, float, float"/>
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float Clamp(this float val, float min, float max)
  {
    return Mathf.Clamp(val, min, max);
  }

  /// <summary>
  /// Extension method for <see cref="Mathf.Clamp(int, int, int"/>
  /// </summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static int Clamp(this int val, int min, int max)
  {
    return Mathf.Clamp(val, min, max);
  }

  /// <summary>
  /// Convert &gt; 360 and &lt; 0 angles to relative 0:360 angles in a unit circle
  /// </summary>
  /// <param name="theta"></param>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static float ClampAngle(this float theta)
  {
    return Mathf.Repeat(theta, 360f);
  }

  /// <summary>
  /// Check if <paramref name="value"/> falls within <paramref name="range"/>, both min and max are inclusive.
  /// </summary>
  /// <returns><see langword="true"/> if <paramref name="value"/> is within <paramref name="range"/></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool InRange(this FloatRange range, float value)
  {
    return value >= range.min && value <= range.max;
  }

  /// <summary>
  /// Check if <paramref name="value"/> falls within <paramref name="range"/>, both min and max are inclusive.
  /// </summary>
  /// <returns><see langword="true"/> if <paramref name="value"/> is within <paramref name="range"/></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public static bool InRange(this IntRange range, int value)
  {
    return value >= range.min && value <= range.max;
  }
}