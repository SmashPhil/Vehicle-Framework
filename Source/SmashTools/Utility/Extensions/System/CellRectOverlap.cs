using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;

namespace SmashTools;

/// <summary>
/// Enumerator wrapper for iterating over all cells within 2 rects, without processing duplicates.
/// </summary>
/// <remarks>
/// NOTE: It is marginally faster to iterate both CellRects individually if the action inside the loop
/// is quick (&lt;1µs).
/// <para/>
/// If duplicates must be avoided or if the inner loop has complex logic, using <see cref="CellRectOverlap.Enumerator"/>
/// will be faster.
/// </remarks>
[PublicAPI]
public readonly struct CellRectOverlap(CellRect cellRect, CellRect otherRect) : IEnumerable<IntVec3>
{
  IEnumerator<IntVec3> IEnumerable<IntVec3>.GetEnumerator()
  {
    return GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  /// <summary>
  /// Get non-allocating enumerator for <see cref="CellRectOverlap"/>
  /// </summary>
  public Enumerator GetEnumerator()
  {
    return new Enumerator(cellRect, otherRect);
  }

  /// <summary>
  /// Non allocating enumerator for <see cref="CellRectOverlap"/>
  /// </summary>
  public struct Enumerator : IEnumerator<IntVec3>
  {
    private readonly CellRect cellRect;
    private readonly CellRect otherRect;
    private readonly Mode mode;

    // CellRect will have 8 boundary lines, we need to separate out which rects extend beyond
    // the overlapping section.
    // NOTE - CellRect subtracts 1 from max limits since max is the indicator of max cell value,
    // not max boundary line which would include the last cell unit. We need to adjust here and
    // decrement 1 when starting at max bounds for cell indices.
    private int maxTopZ, maxBotZ; // Top Bounds
    private int minTopZ, minBotZ; // Bottom Bounds
    private int minLeftX, minRightX; // Left Bounds
    private int maxLeftX, maxRightX; // Right Bounds

    // Needs expanded iteration to catch floating corners when rect is not in a cross pattern
    // and only partially overlaps.
    private RectEdge edge = RectEdge.None;

    private int x, xEnd;
    private int z, zStart, zEnd;
    private Phase phase;

    internal Enumerator(CellRect cellRect, CellRect otherRect)
    {
      this.cellRect = cellRect;
      this.otherRect = otherRect;

      if (cellRect == otherRect)
      {
        mode = Mode.Equal;
      }
      else if (!cellRect.Overlaps(otherRect))
      {
        mode = Mode.NoOverlap;
      }
      else
      {
        mode = Mode.Overlap;
        GatherBoundaryLines();
      }
      Reset();
    }

    public IntVec3 Current { get; private set; }

    object IEnumerator.Current => Current;

    public void Dispose()
    {
    }

    public void Reset()
    {
      switch (mode)
      {
        case Mode.Equal or Mode.NoOverlap:
          phase = Phase.RectA;
          break;
        case Mode.Overlap:
          phase = Phase.Left;
          break;
        default:
          throw new NotImplementedException(mode.ToString());
      }
      SetupNext();
    }

    public bool MoveNext()
    {
      while (phase != Phase.Done)
      {
        if (MoveNextInRange())
          return true;

        AdvanceSection();
        SetupNext();
      }
      return false;
    }

    private bool MoveNextInRange()
    {
      while (x < xEnd)
      {
        if (z >= zEnd)
        {
          Current = new IntVec3(x, 0, z);
          z--;
          return true;
        }

        if (++x >= xEnd)
          break;

        z = zStart;
      }
      return false;
    }

    private void SetupNext()
    {
      if (phase == Phase.Done)
        return;

      int xMin, xMax, zMin, zMax;
      switch (phase)
      {
        case Phase.Left:
          GetLeftRange(out xMin, out xMax, out zMin, out zMax);
          break;
        case Phase.Right:
          GetRightRange(out xMin, out xMax, out zMin, out zMax);
          break;
        case Phase.Top:
          GetTopRange(out xMin, out xMax, out zMin, out zMax);
          break;
        case Phase.Bottom:
          GetBottomRange(out xMin, out xMax, out zMin, out zMax);
          break;
        case Phase.Overlap:
          GetOverlappingRange(out xMin, out xMax, out zMin, out zMax);
          break;
        case Phase.RectA:
          xMin = cellRect.minX;
          xMax = cellRect.maxX + 1;
          zMin = cellRect.maxZ;
          zMax = cellRect.minZ;
          break;
        case Phase.RectB:
          xMin = otherRect.minX;
          xMax = otherRect.maxX + 1;
          zMin = otherRect.maxZ;
          zMax = otherRect.minZ;
          break;
        default:
          throw new NotImplementedException(phase.ToString());
      }
      InitRange(xMin, xMax, zMin, zMax);
    }

    // X is exclusive, Z is inclusive
    private void InitRange(int x0, int x1, int z0, int z1)
    {
      x = x0;
      xEnd = x1;
      z = zStart = z0;
      zEnd = z1;
    }

    private void AdvanceSection()
    {
      switch (mode)
      {
        case Mode.Equal:
          {
            phase = Phase.Done;
          }
          break;
        case Mode.NoOverlap:
          {
            phase = phase switch
            {
              Phase.RectA => Phase.RectB,
              Phase.RectB => Phase.Done,
              _ => throw new NotImplementedException(phase.ToString())
            };
          }
          break;
        case Mode.Overlap:
          {
            phase = phase switch
            {
              Phase.Left => Phase.Right,
              Phase.Right => Phase.Top,
              Phase.Top => Phase.Bottom,
              Phase.Bottom => Phase.Overlap,
              Phase.Overlap => Phase.Done,
              _ => throw new NotImplementedException(phase.ToString())
            };
          }
          break;
        default:
          throw new NotImplementedException(mode.ToString());
      }
    }

    private void GetLeftRange(out int xMin, out int xMax, out int zMin, out int zMax)
    {
      xMin = minLeftX;
      xMax = minRightX;
      zMin = edge == RectEdge.Left ? maxTopZ - 1 : maxBotZ - 1;
      zMax = edge == RectEdge.Left ? minBotZ : minTopZ;
    }

    private void GetRightRange(out int xMin, out int xMax, out int zMin, out int zMax)
    {
      xMin = maxLeftX;
      xMax = maxRightX;
      zMin = edge == RectEdge.Right ? maxTopZ - 1 : maxBotZ - 1;
      zMax = edge == RectEdge.Right ? minBotZ : minTopZ;
    }

    private void GetTopRange(out int xMin, out int xMax, out int zMin, out int zMax)
    {
      xMin = edge == RectEdge.Top ? minLeftX : minRightX;
      xMax = (edge & RectEdge.Top) == RectEdge.Top ? maxRightX : maxLeftX;
      zMin = maxTopZ - 1;
      zMax = maxBotZ;
      switch (edge)
      {
        // Corners mirror limits
        case RectEdge.TopLeft or RectEdge.BottomRight:
          xMin = minLeftX;
          xMax = maxLeftX;
          break;
        case RectEdge.TopRight or RectEdge.BottomLeft:
          xMin = minRightX;
          xMax = maxRightX;
          break;
      }
    }

    private void GetBottomRange(out int xMin, out int xMax, out int zMin, out int zMax)
    {
      xMin = edge == RectEdge.Bottom ? minLeftX : minRightX;
      xMax = (edge & RectEdge.Bottom) == RectEdge.Bottom ? maxRightX : maxLeftX;
      zMin = minTopZ - 1;
      zMax = minBotZ;
      switch (edge)
      {
        // Corners mirror limits
        case RectEdge.TopLeft or RectEdge.BottomRight:
          xMin = minRightX;
          xMax = maxRightX;
          break;
        case RectEdge.TopRight or RectEdge.BottomLeft:
          xMin = minLeftX;
          xMax = maxLeftX;
          break;
      }
    }

    private void GetOverlappingRange(out int xMin, out int xMax, out int zMin, out int zMax)
    {
      xMin = minRightX;
      xMax = maxLeftX;
      zMin = maxBotZ - 1;
      zMax = minTopZ;
    }

    private void GatherBoundaryLines()
    {
      // Top
      if (cellRect.maxZ > otherRect.maxZ)
      {
        maxTopZ = cellRect.maxZ + 1;
        maxBotZ = otherRect.maxZ + 1;
        if (cellRect.Width > otherRect.Width)
          edge |= RectEdge.Top;
      }
      else
      {
        maxTopZ = otherRect.maxZ + 1;
        maxBotZ = cellRect.maxZ + 1;
        if (otherRect.Width > cellRect.Width)
          edge |= RectEdge.Top;
      }

      // Bottom
      if (cellRect.minZ > otherRect.minZ)
      {
        minTopZ = cellRect.minZ;
        minBotZ = otherRect.minZ;
        if (cellRect.Width < otherRect.Width)
          edge |= RectEdge.Bottom;
      }
      else
      {
        minTopZ = otherRect.minZ;
        minBotZ = cellRect.minZ;
        if (otherRect.Width < cellRect.Width)
          edge |= RectEdge.Bottom;
      }

      // Left
      if (cellRect.minX < otherRect.minX)
      {
        minLeftX = cellRect.minX;
        minRightX = otherRect.minX;
        if (cellRect.Height > otherRect.Height)
          edge |= RectEdge.Left;
      }
      else
      {
        minLeftX = otherRect.minX;
        minRightX = cellRect.minX;
        if (otherRect.Height > cellRect.Height)
          edge |= RectEdge.Left;
      }

      // Right
      if (cellRect.maxX < otherRect.maxX)
      {
        maxLeftX = cellRect.maxX + 1;
        maxRightX = otherRect.maxX + 1;
        if (cellRect.Height < otherRect.Height)
          edge |= RectEdge.Right;
      }
      else
      {
        maxLeftX = otherRect.maxX + 1;
        maxRightX = cellRect.maxX + 1;
        if (otherRect.Height < cellRect.Height)
          edge |= RectEdge.Right;
      }
    }

    [Flags]
    private enum RectEdge
    {
      None,
      Left = 1 << 0,
      Right = 1 << 1,
      Top = 1 << 2,
      Bottom = 1 << 3,

      // NOTE - Edges are parts of the rect that are hanging off. If 2 edges are
      // hanging off then the corner will be opposite to the cardinal edge that
      // is hanging off e.g. Top edge + right hanging = top left corner is overlapping.
      BottomLeft = Bottom | Right,
      TopLeft = Top | Right,
      TopRight = Top | Left,
      BottomRight = Bottom | Left,
    }

    private enum Mode
    {
      Equal,
      NoOverlap,
      Overlap
    }

    private enum Phase
    {
      Done,
      Left,
      Right,
      Top,
      Bottom,
      Overlap,

      RectA,
      RectB
    }
  }
}