using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace CoreLib;

[PublicAPI]
public readonly struct EntityRect : IEnumerable<int2>
{
  public readonly int2 position;
  public readonly int2 size;
  public readonly Orientation orientation;

  public EntityRect(int2 position, int2 size, Orientation orientation)
  {
    this.position = position;
    this.size = size;
    this.orientation = orientation;
  }

  public EntityRect(int x, int y, int width, int height, Orientation orientation)
  {
    position = new int2(x, y);
    size = new int2(width, height);
    this.orientation = orientation;
  }

  IEnumerator<int2> IEnumerable<int2>.GetEnumerator()
  {
    return GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  public Enumerator GetEnumerator()
  {
    return new Enumerator(position, size.x, size.y, orientation);
  }

  public struct Enumerator : IEnumerator<int2>
  {
    private readonly int2 position;
    private readonly Orientation orientation;

    private readonly int width;
    private readonly bool evenVehicleWidth;

    private readonly int xMin;
    private readonly int xMax;
    private readonly int yMin;
    private readonly int yMax;

    private readonly int xBias;
    private readonly int yBias;
    
    private int x;
    private int y;

    private int rowCount;
    private int xStart;

    private int2 current;

    internal Enumerator(int2 position, int width, int height, Orientation orientation)
    {
      this.position = position;
      this.orientation = orientation;

      this.width = 0;
      evenVehicleWidth = width.IsEven();
      xBias = 0;
      yBias = 0;
      rowCount = 0;
      xStart = 0;

      current = default;

      if (orientation.IsCardinal)
      {
        int xLength = orientation.IsHorizontal ? height : width;
        int yLength = orientation.IsHorizontal ? width : height;

        xMin = -(xLength - 1) / 2;
        xMax = xMin + xLength - 1;
        yMin = -(yLength - 1) / 2;
        yMax = yMin + yLength - 1;
        if (orientation.IsHorizontal && width.IsEven())
        {
          yMin--;
          yMax--;
        }

        x = xMin - 1;
        y = yMin;
      }
      else
      {
        int diagonalWidth = Mathf.CeilToInt(width / MathUtils.Sqrt2);
        // Even width with even height will result in smaller height + off center root.
        // Pad 1 to ensure the entire vehicle is covered.
        int paddedHeight = height + (width.IsEven() && height.IsEven() ? 1 : 0);
        int diagonalHeight = Mathf.CeilToInt(paddedHeight / MathUtils.Sqrt2);

        bool extraHalfColumn = width.IsEven() && diagonalWidth == Mathf.CeilToInt((width - 1) / MathUtils.Sqrt2);
        bool extraHalfRow = height.IsEven() && diagonalHeight == Mathf.CeilToInt((height - 1) / MathUtils.Sqrt2);

        yMin = -(diagonalWidth - 1);
        yMax = (diagonalWidth - 1) + (extraHalfColumn ? 1 : 0);

        int trimTop = width.IsEven() && !height.IsEven() ? 1 : 0;
        xMin = -(diagonalHeight - 1);
        xMax = diagonalHeight - 1 + (extraHalfRow ? 1 : 0) - trimTop;

        this.width = yMax - yMin + 1;
        yBias = width.IsEven() ? 1 : 0;
        xBias = width.IsEven() && height.IsEven() ? 1 : 0;

        x = 0;
        y = 0;
        PrepareColumn();
      }
    }

    public int2 Current => current;

    object IEnumerator.Current => Current;

    private void PrepareColumn()
    {
      int v = yMin + x + yBias;

      int first = xMin + xBias;
      if ((first ^ v).IsOdd())
      {
        first++;
      }

      int last = xMax;
      if ((last ^ v).IsOdd())
      {
        last--;
      }

      if (first > last)
      {
        rowCount = 0;
        xStart = 0;
        return;
      }

      xStart = first;
      rowCount = ((last - first) >> 1) + 1;
    }

    public bool MoveNext()
    {
      if (orientation.IsCardinal)
      {
        return MoveCardinal();
      }
      return MoveDiagonal();
    }

    private bool MoveCardinal()
    {
      x++;
      if (x > xMax)
      {
        x = xMin;
        y++;
      }
      if (y > yMax)
      {
        return false;
      }

      current = position + new int2(x, y);
      return true;
    }

    private bool MoveDiagonal()
    {
      while (true)
      {
        if (x >= width)
        {
          return false;
        }

        if (y >= rowCount)
        {
          x++;
          y = 0;
          if (x >= width)
          {
            return false;
          }

          PrepareColumn();
          continue;
        }

        int v = yMin + x + yBias;
        int u = xStart + (y << 1) + xBias;

        int x1 = (u + v) >> 1;
        int y1 = (u - v) >> 1;

        current = position + orientation.AsInt switch
        {
          Orientation.NorthEast or Orientation.SouthWest => new int2(x1, y1),
          Orientation.SouthEast or Orientation.NorthWest => new int2(x1, -y1) -
            (evenVehicleWidth ? new int2(1, 1) : default),
          _ => throw new InvalidOperationException(nameof(orientation))
        };

        y++;
        return true;
      }
    }

    public void Reset()
    {
      if (orientation.IsCardinal)
      {
        x = xMin - 1;
        y = yMin;
      }
      else
      {
        x = 0;
        y = 0;
        PrepareColumn();
      }
      current = default;
    }

    public void Dispose()
    {
    }
  }
}
