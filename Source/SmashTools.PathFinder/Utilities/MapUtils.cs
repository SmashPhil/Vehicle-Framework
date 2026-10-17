
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  public static class MapUtils
  {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InBounds(int x, int y, int2 size)
    {
      return (uint)x < size.x && (uint)y < size.y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InBounds(int x, int y, int width, int height)
    {
      return (uint)x < width && (uint)y < height;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool InDiagonalBounds(int v0, int u0, int v1, int u1, int width, int height)
    {
      // Normalize
      int uMin = u0 < u1 ? u0 : u1;
      int uMax = u0 < u1 ? u1 : u0;
      int vMin = v0 < v1 ? v0 : v1;
      int vMax = v0 < v1 ? v1 : v0;

      int maxU = (width - 1) + (height - 1);
      if ((uint)uMin > (uint)maxU || (uint)uMax > (uint)maxU)
        return false;

      return vMin >= MinVAtU(uMin, width, height) &&
             vMax <= MaxVAtU(uMin, width, height) &&
             vMin >= MinVAtU(uMax, width, height) &&
             vMax <= MaxVAtU(uMax, width, height);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MinVAtU(int u, int width, int height)
    {
      int xLow = u - (height - 1);
      if (xLow < 0) xLow = 0;
      return (xLow << 1) - u;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int MaxVAtU(int u, int width, int height)
    {
      int xHigh = u;
      int maxX = width - 1;
      if (xHigh > maxX) xHigh = maxX;
      return (xHigh << 1) - u;
    }
  }
}