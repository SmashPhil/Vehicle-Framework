using System;
using CoreLib.PathFinding;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestFindPathJobWithScalar
  {
    private const int MapWidth = 5;
    private const int MapHeight = 5;

    private static readonly int2 Size = new(MapWidth, MapHeight);

    private struct PathFinderData : IDisposable
    {
      public PathFinder pathFinder;
      public NativeArray<int> pathGrid;

      public void Dispose()
      {
        pathGrid.Dispose();
        pathFinder.Dispose();
      }
    }

    private static void SetGrid(PathFinderData pathData, int[,] grid)
    {
      pathData.pathFinder.NotifyWritingToGrid();
      GridHelper.SetGrid(pathData.pathGrid, grid);
    }

    private static void SetGrid(PathGridScalar scalar, int[,] grid)
    {
      int height = grid.GetLength(0);
      int width = grid.GetLength(1);
      // Grid is converted to path grid rotated and flipped
      // to place 0,0 in bottom left for better visualization.
      for (int i = 0; i < height; i++)
      {
        for (int x = 0; x < width; x++)
        {
          int y = height - 1 - i;
          int index = x + y * width;
          scalar[index] = grid[i, x] != 0;
        }
      }
    }

    private static PathFinderData CreatePathFinderData(int width = MapWidth, int height = MapHeight,
      int2 hitbox = default)
    {
      int2 mapSize = new(width, height);
      NativeArray<int> pathGrid = new(width * height, Allocator.TempJob);
      PathFinder pathFinder = new(new PathFinder.Settings
      {
        mapSize = mapSize,
        hitbox = hitbox,
        pathGrid = pathGrid.AsReadOnly(),
        poolObjects = false
      });
      
      return new PathFinderData { pathFinder = pathFinder, pathGrid = pathGrid };
    }

    [Test]
    public void PathWithScalarDecrease()
    {
      const int H = 1;
      using PathFinderData data = CreatePathFinderData();
      using PathGridScalar scalar = new(Size);
      SetGrid(scalar, new[,]
      {
        { 0, 0, 0, 0, 0, },
        { 0, 0, 0, 0, 0, },
        { H, H, H, H, H, },
        { H, 0, 0, 0, H, },
        { H, 0, 0, 0, H, }
      });
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, }
      });
      PathRequest request = new()
      {
        start = new int3(0, 0, 0),
        end = new int3(4, 0, 0),
        scalar = scalar.ToData(0.15f)
      };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 4 * PathFinder.CostCardinal + 2 * PathFinder.CostDiagonal);
    }

    [Test]
    public void PathWithScalarIncrease()
    {
      const int B = 1;
      using PathFinderData data = CreatePathFinderData();
      using PathGridScalar scalar = new(Size);
      SetGrid(scalar, new[,]
      {
        { 0, 0, 0, 0, 0, },
        { 0, 0, 0, 0, 0, },
        { 0, 0, B, 0, 0, },
        { 0, 0, B, 0, 0, },
        { 0, 0, B, 0, 0, }
      });
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, }
      });
      PathRequest request = new()
      {
        start = new int3(0, 0, 0),
        end = new int3(4, 0, 0),
        scalar = scalar.ToData(10)
      };
      Path path = data.pathFinder.FindPath(request);
      Assert.IsTrue(path.IsValid);
      GridHelper.ValidateOptimalPath(in request, path, expected: 2 * PathFinder.CostCardinal + 4 * PathFinder.CostDiagonal);
    }

    [Test]
    public void ImpassableWithMaskMultiCell()
    {
      const int MapLength = 10;
      const int B = 1;

      int2 mapSize = new(MapLength, MapLength);
      using PathFinderData data = CreatePathFinderData(mapSize.x, mapSize.y, hitbox: new int2(3, 3));
      using PathGridScalar scalar = new(mapSize);
      SetGrid(scalar, new[,]
      {
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, B, B, B, B, B, B, B, B, 0 },
        { 0, B, 0, 0, 0, 0, 0, 0, B, 0 },
        { 0, B, 0, 0, 0, 0, 0, 0, B, 0 },
        { 0, B, 0, 0, 0, 0, 0, 0, B, 0 },
        { 0, B, 0, 0, 0, 0, 0, 0, B, 0 },
        { 0, B, 0, 0, 0, 0, 0, 0, B, 0 },
        { 0, B, 0, 0, 0, 0, 0, 0, B, 0 },
        { 0, B, 0, 0, 0, 0, 0, 0, B, 0 },
        { 0, B, 0, 0, 0, 0, 0, 0, B, 0 }
      });
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
      });
      PathRequest request = new()
      {
        start = new int3(1, 0, 1),
        end = new int3(8, 0, 1),
        scalar = scalar.ToData(0.1f)
      };
      Path path = data.pathFinder.FindPath(request);
      Assert.IsTrue(path.IsValid);
      GridHelper.ValidateOptimalPath(in request, path, expected: 21 * PathFinder.CostCardinal);
    }
  }
}