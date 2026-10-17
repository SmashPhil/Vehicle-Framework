using System;
using CoreLib.PathFinding;
using DevTools.Testing;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_FindPathJobWithMask
{
  private const int X = PathFinder.ImpassableCost;
  private const int MapWidth = 5;
  private const int MapHeight = 5;

  private static readonly int2 Size = new(5, 5);

  [UsedImplicitly]
  private static int2[] hitboxes = { new(2, 3), new(3, 2), new(3, 3) };

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

  private static PathFinderData CreatePathFinderData(int width = MapWidth, int height = MapHeight,
    int2 hitbox = default)
  {
    NativeArray<int> pathGrid = new(width * height, Allocator.TempJob);
    PathFinder pathFinder = new(new PathFinder.Settings
    {
      mapSize = new int2(width, height),
      hitbox = hitbox,
      pathGrid = pathGrid.AsReadOnly(),
      poolObjects = false
    });
    return new PathFinderData { pathFinder = pathFinder, pathGrid = pathGrid };
  }

  // Because the mask doesn't make the cell impassable, corner cutting against the high-cost
  // cell is still a valid path.
  // -----------
  //  0 0 0 0 0
  //  0 0 + 0 0
  //  0 + B + 0
  //  + 0 B 0 +
  //  + 0 B 0 +
  [Test]
  public void PathWithMask()
  {
    const int B = 50;
    using PathFinderData data = CreatePathFinderData();
    using MockBreach mask = new(Size);
    GridHelper.SetGrid(mask.Array, new[,]
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
      mask = mask
    };
    Path path = data.pathFinder.FindPath(request);
    GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
    GridHelper.ValidateOptimalPath(in request, path, expected: 2 * PathFinder.CostCardinal + 4 * PathFinder.CostDiagonal);
  }

  // Mask enables an 'unreachable' location by overwriting impassable cells with a passable cost.
  [Test]
  public void PassableWithMask()
  {
    const int B = 1;
    using PathFinderData data = CreatePathFinderData();
    using MockBreach mask = new(Size);
    GridHelper.SetGrid(mask.Array, new[,]
    {
        { 0, 0, 0, 0, 0, },
        { 0, 0, 0, 0, 0, },
        { 0, 0, B, 0, 0, },
        { 0, 0, 0, 0, 0, },
        { 0, 0, 0, 0, 0, }
      });
    SetGrid(data, new[,]
    {
        { 1, 1, X, 1, 1, },
        { 1, 1, X, 1, 1, },
        { 1, 1, X, 1, 1, },
        { 1, 1, X, 1, 1, },
        { 1, 1, X, 1, 1, }
      });
    PathRequest request = new()
    {
      start = new int3(0, 0, 2),
      end = new int3(4, 0, 2),
      mask = mask
    };
    Path path = data.pathFinder.FindPath(request);
    Assert.IsTrue(path.IsValid);
  }

  // Mask invalidates the 'reachable' location by overwriting passable cells with an impassable cost.
  [Test]
  public void ImpassableWithMask()
  {
    const int B = PathFinder.ImpassableCost;
    using PathFinderData data = CreatePathFinderData();
    using MockBreach mask = new(Size);
    GridHelper.SetGrid(mask.Array, new[,]
    {
        { 0, 0, B, 0, 0, },
        { 0, 0, B, 0, 0, },
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
      mask = mask
    };
    Path path = data.pathFinder.FindPath(request);
    Assert.IsFalse(path.IsValid);
  }

  [Test]
  public void ImpassableWithMaskMultiCell()
  {
    const int MapLength = 10;
    const int B = PathFinder.ImpassableCost;

    using PathFinderData data = CreatePathFinderData(MapLength, MapLength, hitbox: new int2(2, 4));
    using MockBreach mask = new(new int2(MapLength, MapLength));
    GridHelper.SetGrid(mask.Array, new[,]
    {
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 }
      });
    SetGrid(data, new[,]
    {
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 }
      });
    PathRequest request = new()
    {
      start = new int3(1, 0, 1),
      end = new int3(7, 0, 1),
      mask = mask
    };
    Path path = data.pathFinder.FindPath(request);
    Assert.IsFalse(path.IsValid);
  }

  [Test, Disabled]
  public void PathThroughMaskHorizontal([ParametersSource(nameof(hitboxes))] int2 hitbox)
  {
    const int MapLength = 10;
    const int B = 100;

    int2 mapSize = new(MapLength, MapLength);
    using PathFinderData data = CreatePathFinderData(MapLength, MapLength, hitbox);
    using MockBreach mask = new(mapSize);
    GridHelper.SetGrid(mask.Array, new[,]
    {
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { B, B, B, B, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 }
      });
    SetGrid(data, new[,]
    {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { X, X, X, X, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 }
      });
    PathRequest request = new()
    {
      start = new int3(1, 0, 1),
      end = new int3(8, 0, 1),
      mask = mask
    };
    Path path = data.pathFinder.FindPath(request);
    Assert.IsTrue(path.IsValid);
    GridHelper.ValidatePathResult(data.pathFinder.Snapshot, mapSize, request, path);
    Assert.AreEqual(expected: 5 * PathFinder.CostCardinal + 2 * PathFinder.CostDiagonal, GridHelper.PathCost(in request, path));
  }

  [Test, Disabled]
  public void PathThroughVertical([ParametersSource(nameof(hitboxes))] int2 hitbox)
  {
    const int MapLength = 10;
    const int B = 100;

    int2 mapSize = new(MapLength, MapLength);
    using PathFinderData data = CreatePathFinderData(MapLength, MapLength, hitbox);
    using MockBreach mask = new(mapSize);
    GridHelper.SetGrid(mask.Array, new[,]
    {
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { B, B, B, B, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 }
      });
    SetGrid(data, new[,]
    {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { X, X, X, X, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 }
      });
    PathRequest request = new()
    {
      start = new int3(1, 0, 1),
      end = new int3(1, 0, 8),
      mask = mask
    };
    Path path = data.pathFinder.FindPath(request);
    Assert.IsTrue(path.IsValid);
    GridHelper.ValidatePathResult(data.pathFinder.Snapshot, mapSize, request, path);
    Assert.AreEqual(expected: 5 * PathFinder.CostCardinal + 2 * PathFinder.CostDiagonal, GridHelper.PathCost(in request, path));
  }

  private class MockBreach : PathGridMask
  {
    public MockBreach(int2 mapSize) : base(mapSize)
    {
    }

    public NativeArray<int> Array => array;
  }
}
