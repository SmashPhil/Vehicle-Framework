using System;
using CoreLib.PathFinding;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestFindPathJobWithHeuristics
  {
    private const HeuristicWeight O = HeuristicWeight.Normal;
    private const HeuristicWeight P = HeuristicWeight.Prefer;
    private const HeuristicWeight A = HeuristicWeight.Avoid;

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

    private static  PathFinderData CreatePathFinderData(int width = MapWidth, int height = MapHeight,
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

    [Test]
    public void PathWithHighway()
    {
      using PathFinderData data = CreatePathFinderData();
      using MockHeuristicGrid grid = new(Size);
      GridHelper.SetGrid(grid.Array, new[,]
      {
        { O, O, O, O, O, },
        { O, O, O, O, O, },
        { P, P, P, P, P, },
        { P, A, O, A, P, },
        { P, A, O, A, P, }
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
        heuristic = new HeuristicGrid.Data(grid)
      };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 4 * PathFinder.CostCardinal + 2 * PathFinder.CostDiagonal);
    }

    [Test]
    public void PathWithAvoidal()
    {
      using PathFinderData data = CreatePathFinderData();
      using MockHeuristicGrid grid = new(Size);
      GridHelper.SetGrid(grid.Array, new[,]
      {
        { O, O, O, O, O, },
        { O, O, O, O, O, },
        { O, O, A, O, O, },
        { O, O, A, O, O, },
        { O, O, A, O, O, }
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
        heuristic = new HeuristicGrid.Data(grid)
      };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 2 * PathFinder.CostCardinal + 4 * PathFinder.CostDiagonal);
    }

    private class MockHeuristicGrid : HeuristicGrid
    {
      public MockHeuristicGrid(int2 mapSize) : base(mapSize)
      {
      }

      public NativeArray<HeuristicWeight> Array => array;
    }
  }
}