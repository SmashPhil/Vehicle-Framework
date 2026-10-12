using System;
using System.Collections.Generic;
using CoreLib.Collections;
using CoreLib.PathFinding;
using DevTools.Testing;
using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_FindPathJobWithOffsets
{
  private const int MapWidth = 5;
  private const int MapHeight = 5;

  private static readonly int2 Size = new(5, 5);

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
  public void PathWithOffset()
  {
    const int B = 100;
    using PathFinderData data = CreatePathFinderData();
    using MockBoolGrid boolGrid = new(Size, value: true);
    GridHelper.SetBitArray(boolGrid.BitArray, new[,]
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
      bitOffsets = new List<BitOffset> { boolGrid.BitOffset }
    };
    Path path = data.pathFinder.FindPath(request);
    GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
    GridHelper.ValidateOptimalPath(in request, path, expected: 2 * PathFinder.CostCardinal + 4 * PathFinder.CostDiagonal);
  }

  [Test]
  public void PathWithOffsetMultiCell()
  {
    const int MapLength = 10;
    const int B = 100;

    int2 mapSize = new(MapLength, MapLength);
    using PathFinderData data = CreatePathFinderData(MapLength, MapLength, hitbox: new int2(3, 3));
    using MockBoolGrid boolGrid = new(mapSize, value: true);
    GridHelper.SetBitArray(boolGrid.BitArray, new[,]
    {
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, B, B, B, B, 0, 0, 0 },
        { 0, 0, 0, B, B, B, B, 0, 0, 0 },
        { 0, 0, 0, B, B, B, B, 0, 0, 0 },
        { 0, 0, 0, B, B, B, B, 0, 0, 0 },
        { 0, 0, 0, B, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, B, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, B, 0, 0, 0, 0, 0, 0 }
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
      end = new int3(5, 0, 1),
      bitOffsets = new List<BitOffset> { boolGrid.BitOffset }
    };
    Path path = data.pathFinder.FindPath(request);
    GridHelper.ValidatePathResult(data.pathFinder.Snapshot, mapSize, request, path);
    GridHelper.ValidateOptimalPath(in request, path, expected: 24 * PathFinder.CostCardinal);
  }

  private class MockBoolGrid : PathBoolGrid
  {
    public MockBoolGrid(int2 mapSize, bool value) : base(mapSize)
    {
      SetAll(value);
    }

    public NativeBitArraySlim BitArray => array;

    public BitOffset BitOffset => new()
    {
      boolGrid = this,
      cost = new Cost { set = 500 }
    };

    private void SetAll(bool value)
    {
      for (int i = 0; i < array.Length; i++)
      {
        array.Set(i, value);
      }
    }
  }
}
