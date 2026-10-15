using System.Linq;
using CoreLib;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SmashTools.PathFinder
{
  internal readonly struct SumTableCostProvider : IPathCostProvider
  {
    private const int ImpassableCost = PathFinder.ImpassableCost;

    private readonly ulong sizeFactorCardinal;
    private readonly ulong sizeFactorDiagonal;

    private readonly int strideCardinal;
    private readonly int strideDiagonal;

    private readonly Coords footprintVertical;
    private readonly Coords footprintHorizontal;
    private readonly Coords footprintDiagonalUp;
    private readonly Coords footprintDiagonalDown;

    [ReadOnly]
    private readonly NativeArray<int>.ReadOnly cardinal;
    [ReadOnly]
    private readonly NativeArray<int>.ReadOnly cardinalBlocked;
    [ReadOnly]
    private readonly NativeArray<int>.ReadOnly diagonal;
    [ReadOnly]
    private readonly NativeArray<int>.ReadOnly diagonalBlocked;

    private readonly struct Coords
    {
      private readonly int x0;
      private readonly int y0;
      private readonly int x1;
      private readonly int y1;

      public Coords(int x0, int y0, int x1, int y1)
      {
        this.x0 = x0;
        this.y0 = y0;
        this.x1 = x1;
        this.y1 = y1;
      }

      public void GetRect(int x, int y, out int px0, out int py0, out int px1, out int py1)
      {
        px0 = x + x0;
        py0 = y + y0;
        px1 = px0 + x1;
        py1 = py0 + y1;
      }
    }

    public SumTableCostProvider(SumAreaTable cardinal, SumAreaTable diagonal, int2 hitbox)
    {
      strideCardinal = cardinal.Stride;
      strideDiagonal = diagonal.Stride;
      this.cardinal = cardinal.Table;
      cardinalBlocked = cardinal.BlockTable;
      this.diagonal = diagonal.Table;
      diagonalBlocked = diagonal.BlockTable;

      sizeFactorCardinal = (1UL << 32) / (ulong)(hitbox.x * hitbox.y);
      int diagonalCellCount = new EntityRect(0, 0, hitbox.x, hitbox.y, new Orientation(Orientation.NorthEast)).Count();
      sizeFactorDiagonal = (1UL << 32) / (ulong)diagonalCellCount;
      footprintVertical = GetCoords(hitbox, Octant.North);
      footprintHorizontal = GetCoords(hitbox, Octant.East);
      footprintDiagonalUp = GetCoords(hitbox, Octant.NorthEast);
      footprintDiagonalDown = GetCoords(hitbox, Octant.SouthEast);
    }

    private static Coords GetCoords(int2 hitbox, int heading)
    {
      int x0, y0, x1, y1;
      if (Octant.IsCardinal(heading))
      {
        (int sx, int sy) = Octant.IsVertical(heading)
          ? (hitbox.x, hitbox.y)
          : (hitbox.y, hitbox.x);

        int left = (sx - 1) >> 1;
        int down = hitbox.x % 2 == 0 ? sy >> 1 : (sy - 1) >> 1;
        x0 = -left;
        y0 = -down;
        x1 = sx - 1;
        y1 = sy - 1;
      }
      else
      {
        int width = hitbox.x;
        int height = hitbox.y;

        int diagonalWidth = Mathf.CeilToInt(width / MathUtils.Sqrt2);
        int paddedHeight = height + (width.IsEven() && height.IsEven() ? 1 : 0);
        int diagonalHeight = Mathf.CeilToInt(paddedHeight / MathUtils.Sqrt2);

        bool extraHalfColumn = width.IsEven() && diagonalWidth == Mathf.CeilToInt((width - 1) / MathUtils.Sqrt2);
        bool extraHalfRow = height.IsEven() && diagonalHeight == Mathf.CeilToInt((height - 1) / MathUtils.Sqrt2);

        int vMin = -(diagonalWidth - 1);
        int vMax = (diagonalWidth - 1) + (extraHalfColumn ? 1 : 0);

        int trimTop = width.IsEven() && !height.IsEven() ? 1 : 0;
        int uMin = -(diagonalHeight - 1);
        int uMax = diagonalHeight - 1 + (extraHalfRow ? 1 : 0) - trimTop;

        int vBias = width.IsEven() ? 1 : 0;
        int uBias = height.IsEven() ? 1 : 0;

        vMin += vBias;
        vMax += vBias;
        uMin += uBias;
        uMax += uBias;

        if ((heading & 3) == 1)
        {
          x0 = vMin;
          y0 = uMin;
          x1 = vMax - vMin;
          y1 = uMax - uMin;
        }
        else
        {
          x0 = uMin;
          y0 = vMin;
          x1 = uMax - uMin;
          y1 = vMax - vMin;
        }
      }
      return new Coords(x0, y0, x1, y1);
    }

    private int SumCardinalPathCost(int x, int y, int heading, int2 mapSize)
    {
      int x0, y0, x1, y1;
      if (Octant.IsVertical(heading))
      {
        footprintVertical.GetRect(x, y, out x0, out y0, out x1, out y1);
      }
      else
      {
        footprintHorizontal.GetRect(x, y, out x0, out y0, out x1, out y1);
      }

      if (!MapUtils.InBounds(x0, y0, mapSize) || !MapUtils.InBounds(x1, y1, mapSize) ||
          SumAreaTable.GetSum(cardinalBlocked, strideCardinal, x0, y0, x1, y1) > 0)
      {
        return ImpassableCost;
      }

      // Using multiplication and bit shift for more performant division.
      // cost = pathCost / (sx * sy)
      int pathCost = SumAreaTable.GetSum(cardinal, strideCardinal, x0, y0, x1, y1);
      ulong prod = (ulong)pathCost * sizeFactorCardinal;
      return (int)((prod + (1UL << 31)) >> 32);
    }

    private int SumDiagonalPathCost(int x, int y, int heading, int2 mapSize)
    {
      int x0, y0, x1, y1;

      int u = x + y;
      int v = x - y;

      // NorthEast or SouthWest
      if ((heading & 3) == 1)
      {
        footprintDiagonalUp.GetRect(v, u, out x0, out y0, out x1, out y1);
      }
      // SouthEast or NorthWest
      else
      {
        footprintDiagonalDown.GetRect(v, u, out x0, out y0, out x1, out y1);
      }

      if (!MapUtils.InDiagonalBounds(x0, y0, x1, y1, mapSize.x, mapSize.y))
      {
        return ImpassableCost;
      }

      int vShift = mapSize.y - 1;
      x0 += vShift;
      x1 += vShift;

      int diagSize = strideDiagonal - 1;
      
      if (!MapUtils.InBounds(x0, y0, diagSize, diagSize) || !MapUtils.InBounds(x1, y1, diagSize, diagSize) ||
          SumAreaTable.GetSum(diagonalBlocked, strideDiagonal, x0, y0, x1, y1) > 0)
      {
        return ImpassableCost;
      }

      // Using multiplication and bit shift for more performant division.
      // cost = pathCost / (sx * sy)
      int pathCost = SumAreaTable.GetSum(diagonal, strideDiagonal, x0, y0, x1, y1);
      ulong prod = (ulong)pathCost * sizeFactorDiagonal;
      return (int)((prod + (1UL << 31)) >> 32);
    }

    public int GetPathCost(int x, int y, int heading, in PathData pathData)
    {
      return Octant.IsCardinal(heading)
        ? SumCardinalPathCost(x, y, heading, pathData.mapSize)
        : SumDiagonalPathCost(x, y, heading, pathData.mapSize);
    }
  }
}