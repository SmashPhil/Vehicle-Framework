using System;
using System.Collections;
using CoreLib;
using CoreLib.PathFinding;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestFindPathJobMultiCell
  {
    private const int X = PathFinder.ImpassableCost;
    private const int N = -1;
    private const int MapWidth = 10;
    private const int MapHeight = 10;

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

    private PathFinderData CreatePathFinderData(int2 hitbox, Allocator allocator = Allocator.TempJob)
    {
      return CreatePathFinderData(hitbox, Size, allocator);
    }

    private static PathFinderData CreatePathFinderData(int2 hitbox, int2 size, Allocator allocator = Allocator.TempJob)
    {
      NativeArray<int> pathGrid = new(size.x * size.y, allocator);
      PathFinder pathFinder = new(new PathFinder.Settings
      {
        mapSize = size,
        hitbox = hitbox,
        pathGrid = pathGrid.AsReadOnly(),
        poolObjects = false
      });
      return new PathFinderData { pathFinder = pathFinder, pathGrid = pathGrid };
    }

    [Test]
    public void OutOfBounds()
    {
      using PathFinderData data = CreatePathFinderData(new int2(3, 3));
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
      PathRequest request = new() { start = new int3(2, 0, 2), end = new int3(2, 0, 0) };
      Path path = data.pathFinder.FindPath(request);
      Assert.IsFalse(path.IsValid);
    }

    [Test]
    public void DestinationInvalidOdd()
    {
      using PathFinderData data = CreatePathFinderData(new int2(1, 3));
      SetGrid(data, new[,]
      {
        { X, X, X, X, X, X, X, X, X, X },
        { X, X, X, X, X, X, X, X, X, X },
        { 1, 1, X, X, 1, 1, X, X, 1, 1 },
        { 1, 1, X, X, 1, 1, X, X, 1, 1 },
        { 1, 1, X, X, 1, 1, X, X, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(4, 0, 1), end = new int3(4, 0, 7) };
      Path path = data.pathFinder.FindPath(request);
      Assert.IsFalse(path.IsValid);
    }

    [Test]
    public void DestinationInvalidEven()
    {
      using PathFinderData data = CreatePathFinderData(new int2(2, 4));
      SetGrid(data, new[,]
      {
        { X, X, X, X, X, X, X, X, X, X },
        { X, X, X, X, X, X, X, X, X, X },
        { 1, 1, X, X, 1, 1, X, X, 1, 1 },
        { 1, 1, X, X, 1, 1, X, X, 1, 1 },
        { 1, 1, X, X, 1, 1, X, X, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(4, 0, 1), end = new int3(4, 0, 7) };
      Path path = data.pathFinder.FindPath(request);
      Assert.IsFalse(path.IsValid);
    }

    [Test]
    public void ImpassableWallOdd()
    {
      using PathFinderData data = CreatePathFinderData(new int2(1, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(1, 0, 1), end = new int3(8, 0, 1) };
      Path path = data.pathFinder.FindPath(request);
      Assert.IsFalse(path.IsValid);
    }

    [Test]
    public void ImpassableWallEven()
    {
      using PathFinderData data = CreatePathFinderData(new int2(2, 4));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(1, 0, 1), end = new int3(7, 0, 1) };
      Path path = data.pathFinder.FindPath(request);
      Assert.IsFalse(path.IsValid);
    }

    [Test]
    public void TightCorridor()
    {
      using PathFinderData data = CreatePathFinderData(new int2(3, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(1, 0, 1), end = new int3(8, 0, 1) };
      Path path = data.pathFinder.FindPath(request);
      Assert.IsFalse(path.IsValid);
    }

    [Test]
    public void CornerCut()
    {
      using PathFinderData data = CreatePathFinderData(new int2(3, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, X, X, X, X, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(1, 0, 8), end = new int3(8, 0, 8) };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 21 * PathFinder.CostCardinal);
    }

    [Test]
    public void AverageCost()
    {
      using PathFinderData data = CreatePathFinderData(new int2(3, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 9, 9, 9, 9, 1, 1, 1 },
        { 1, 1, 1, 9, 9, 9, 9, 1, 1, 1 },
        { 1, 1, 1, 9, 9, 9, 9, 1, 1, 1 },
        { 1, 1, 1, 9, 9, 9, 9, 1, 1, 1 },
        { 1, 1, 1, 9, 9, 9, 9, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(1, 0, 1), end = new int3(8, 0, 1) };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 13 * PathFinder.CostCardinal + 2 * PathFinder.CostDiagonal);
    }

    [Test]
    public void NegativeCost()
    {
      using PathFinderData data = CreatePathFinderData(new int2(3, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, 1, N, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, N, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, N, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, N, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, N, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, N, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, N, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, N, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, N, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(1, 0, 4), end = new int3(8, 0, 4) };
      Path path = data.pathFinder.FindPath(request);
      Assert.IsFalse(path.IsValid);
    }

    [Test]
    public void EvenWidthEastOnNorthEdge()
    {
      using PathFinderData data = CreatePathFinderData(new int2(2, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(1, 0, 3), end = new int3(8, 0, 3) };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 7 * PathFinder.CostCardinal);
    }

    [Test]
    public void EvenWidthWestOnNorthEdge()
    {
      using PathFinderData data = CreatePathFinderData(new int2(2, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(8, 0, 3), end = new int3(1, 0, 3) };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 7 * PathFinder.CostCardinal);
    }

    [Test]
    public void EvenWidthEastOnSouthEdge()
    {
      using PathFinderData data = CreatePathFinderData(new int2(2, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(1, 0, 7), end = new int3(8, 0, 7) };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 7 * PathFinder.CostCardinal);
    }

    [Test]
    public void EvenWidthWestOnSouthEdge()
    {
      using PathFinderData data = CreatePathFinderData(new int2(2, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(8, 0, 7), end = new int3(1, 0, 7) };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 7 * PathFinder.CostCardinal);
    }

    [Test]
    public void EvenWidthNorthOnEastEdge()
    {
      using PathFinderData data = CreatePathFinderData(new int2(2, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, X, X, X, X },
        { 1, 1, 1, 1, X, X, X, X, X, X },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(2, 0, 1), end = new int3(2, 0, 8) };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 7 * PathFinder.CostCardinal);
    }

    [Test]
    public void EvenWidthSouthOnEastEdge()
    {
      using PathFinderData data = CreatePathFinderData(new int2(2, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, X, X, X, X, X },
        { 1, 1, 1, 1, X, X, X, X, X, X },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(2, 0, 1), end = new int3(2, 0, 8) };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 7 * PathFinder.CostCardinal);
    }

    [Test]
    public void EvenWidthNorthOnWestEdge()
    {
      using PathFinderData data = CreatePathFinderData(new int2(2, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { X, X, X, X, X, X, 1, 1, 1, 1 },
        { X, X, X, X, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(6, 0, 1), end = new int3(6, 0, 8) };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 7 * PathFinder.CostCardinal);
    }

    [Test]
    public void EvenWidthSouthOnWestEdge()
    {
      using PathFinderData data = CreatePathFinderData(new int2(2, 3));
      SetGrid(data, new[,]
      {
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { X, X, X, X, X, X, 1, 1, 1, 1 },
        { X, X, X, X, X, X, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }
      });
      PathRequest request = new() { start = new int3(6, 0, 8), end = new int3(6, 0, 1) };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 7 * PathFinder.CostCardinal);
    }

    [Test]
    public void EvenWidthCornerEastRightCorner()
    {
      const int B = 3;

      int2 size = new(20, 20);
      int2 hitbox = new(2, 4);
      using PathFinderData data = CreatePathFinderData(hitbox, size);
      SetGrid(data, new[,]
      {
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, X, X, X, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, X, X, X, 1, X, X, X, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, X, X, X, 1, X, X, X, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, X, X, X, 1, 1, X, X, X, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, X, X, X, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
      });
      PathRequest request = new()
      {
        start = new int3(1, 0, 10),
        end = new int3(17, 0, 10),
        rotation = Orientation.East
      };
      Path path = data.pathFinder.FindPath(request);
      Assert.IsNotNull(path);
      Assert.IsTrue(path.IsValid);
      GridHelper.PrintPath(data.pathGrid, size, in request, path);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, size, request, path);
      GridHelper.ValidateOptimalPath(in request, path,
        expected: 16 * PathFinder.CostCardinal + 3 * PathFinder.CostDiagonal);

      // Verify no parts of the hitbox clip in short pockets of low-cost cells
      path.ResetPathToStart();
      var nodes = path.Nodes;
      Path.Node prev = nodes[0];
      for (int i = 1; i < nodes.Count; i++)
      {
        Path.Node current = nodes[i];
        Orientation orientation = PathUtils.DirectionToCell(prev.ToInt2(), current.ToInt2());
        EntityRect rect = new(current.x, current.y, hitbox.x, hitbox.y, orientation);
        foreach (int2 cell in rect)
        {
          int index = cell.x + cell.y * size.x;
          Assert.IsTrue(data.pathGrid[index] < PathFinder.ImpassableCost, $"Impassable cell in hitbox at {cell}");
        }
      }
    }

    [Test]
    public void EvenWidthCornerWestRightCorner()
    {
      const int B = 3;

      int2 size = new(20, 20);
      using PathFinderData data = CreatePathFinderData(new int2(2, 4), size);
      SetGrid(data, new[,]
      {
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, X, X, X, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, X, X, X, 1, X, X, X, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, X, X, X, 1, X, X, X, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, X, X, X, 1, 1, X, X, X, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, X, X, X, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
      });
      PathRequest request = new()
      {
        start = new int3(17, 0, 10),
        end = new int3(1, 0, 10),
        rotation = Orientation.West
      };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.PrintPath(data.pathGrid, size, in request, path);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, size, request, path);
      GridHelper.ValidateOptimalPath(in request, path,
        expected: 14 * PathFinder.CostCardinal + 4 * PathFinder.CostDiagonal);
    }

    [Test]
    public void EvenWidthCornerEastLeftCorner()
    {
      const int B = 3;

      int2 size = new(20, 20);
      using PathFinderData data = CreatePathFinderData(new int2(2, 4), size);
      SetGrid(data, new[,]
      {
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { 1, 1, 1, 1, 1, X, X, X, X, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, X, X, X, X, 1, X, X, X, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, X, X, X, X, 1, X, X, X, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, X, X, X, X, 1, 1, X, X, X, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, X, X, X, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
      });
      PathRequest request = new()
      {
        start = new int3(1, 0, 10),
        end = new int3(17, 0, 10),
        rotation = Orientation.East
      };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.PrintPath(data.pathGrid, size, in request, path);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, size, request, path);
      GridHelper.ValidateOptimalPath(in request, path,
        expected: 9 * PathFinder.CostCardinal + 8 * PathFinder.CostDiagonal);
    }

    [Test]
    public void EvenWidthCornerWestLeftCorner()
    {
      const int B = 3;

      int2 size = new(20, 20);
      using PathFinderData data = CreatePathFinderData(new int2(2, 4), size);
      SetGrid(data, new[,]
      {
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { 1, 1, 1, 1, 1, X, X, X, X, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, X, X, X, X, 1, X, X, X, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, X, X, X, X, 1, X, X, X, 1, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, X, X, X, X, 1, 1, X, X, X, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, X, X, X, 1, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
        { B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B },
      });
      PathRequest request = new()
      {
        start = new int3(17, 0, 10),
        end = new int3(1, 0, 10),
        rotation = Orientation.West
      };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.PrintPath(data.pathGrid, size, in request, path);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, size, request, path);
      GridHelper.ValidateOptimalPath(in request, path,
        expected: 8 * PathFinder.CostCardinal + 8 * PathFinder.CostDiagonal);
    }

    [UnityTest]
    public IEnumerator ScheduleJob()
    {
      using PathFinderData data = CreatePathFinderData(new int2(3, 3), Allocator.Persistent);
      GameObject go = new("Test Dispatcher");
      try
      {
        go.AddComponent<JobDispatchManager>();
        SetGrid(data, new[,]
        {
          { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
          { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
          { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 },
          { 1, 1, 1, X, X, X, X, 1, 1, 1 },
          { 1, 1, 1, X, X, X, X, 1, 1, 1 },
          { 1, 1, 1, X, X, X, X, 1, 1, 1 },
          { 1, 1, 1, X, X, X, X, 1, 1, 1 },
          { 1, 1, 1, X, 1, 1, 1, 1, 1, 1 },
          { 1, 1, 1, X, 1, 1, 1, 1, 1, 1 },
          { 1, 1, 1, X, 1, 1, 1, 1, 1, 1 }
        });
        PathRequest request = new() { start = new int3(1, 0, 2), end = new int3(5, 0, 1) };
        using PathReceipt receipt = data.pathFinder.RequestPath(request);

        while (!receipt.IsCompleted)
        {
          yield return null;
        }

        Path path = receipt.GetPath();
        Assert.IsTrue(path.IsValid);
        GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      }
      finally
      {
        if (go)
        {
          Object.DestroyImmediate(go);
        }
      }
    }
  }
}
