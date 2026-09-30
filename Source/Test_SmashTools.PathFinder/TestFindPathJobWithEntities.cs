using System;
using System.Collections;
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
  internal class TestFindPathJobWithEntities
  {
    private const short P = 1;
    private const short E = 2;
    private const int O = -1;

    private const int OverlapCost = PathFinder.ImpassableCost;

    private const int MapWidth = 5;
    private const int MapHeight = 5;

    private const int Length = MapWidth * MapHeight;
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

    private PathFinderData CreatePathFinderData(int2 hitbox = default, Allocator allocator = Allocator.TempJob)
    {
      NativeArray<int> pathGrid = new(Length, allocator);
      GridHelper.SetDefaultValues(pathGrid, 1);
      PathFinder pathFinder = new(new PathFinder.Settings
      {
        mapSize = Size,
        hitbox = hitbox,
        pathGrid = pathGrid.AsReadOnly(),
        poolObjects = false
      });
      return new PathFinderData { pathFinder = pathFinder, pathGrid = pathGrid };
    }

    //  0 0 0 0 0
    //  0 0 0 0 0
    //  0 0 + 0 0
    //  0 + 0 0 0
    //  P 0 0 0 0
    [Test]
    public void PathNoEntities()
    {
      using NativeArray<int> entityGrid = new(Length, Allocator.TempJob);
      using PathFinderData data = CreatePathFinderData();
      GridHelper.SetGrid(entityGrid, new[,]
      {
        { O, O, O, O, O },
        { O, O, O, O, O },
        { O, O, O, O, O },
        { O, O, O, O, O },
        { P, O, O, O, O }
      });
      PathRequest request = new()
      {
        start = new int3(0, 0, 0),
        end = new int3(2, 0, 2),
        entityCost = new EntityConfig(P, entityGrid.AsReadOnly(), OverlapCost)
      };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 2 * PathFinder.CostDiagonal);
    }

    //  0 0 0 0 0
    //  0 0 0 0 0
    //  + + + 0 0
    //  + E 0 0 0
    //  P 0 0 0 0
    [Test]
    public void PathAroundEntity()
    {
      using NativeArray<int> entityGrid = new(Length, Allocator.TempJob); 
      using PathFinderData data = CreatePathFinderData();
      GridHelper.SetGrid(entityGrid, new[,]
      {
        { O, O, O, O, O },
        { O, O, O, O, O },
        { O, O, O, O, O },
        { O, E, O, O, O },
        { P, O, O, O, O }
      });
      PathRequest request = new()
      {
        start = new int3(0, 0, 0),
        end = new int3(2, 0, 2),
        entityCost = new EntityConfig(P, entityGrid.AsReadOnly(), OverlapCost)
      };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 4 * PathFinder.CostCardinal);
    }

    //  0 0 0 0 0
    //  0 0 0 0 0
    //  + + + 0 0
    //  + E 0 0 0
    //  P 0 0 0 0
    [Test]
    public void PathNearEntity()
    {
      using NativeArray<int> entityGrid = new(Length, Allocator.TempJob);
      using PathFinderData data = CreatePathFinderData();
      GridHelper.SetGrid(entityGrid, new[,]
      {
        { O, O, O, O, O },
        { O, O, O, O, O },
        { E, O, O, O, O },
        { O, O, O, O, O },
        { P, O, O, O, O }
      });
      PathRequest request = new()
      {
        start = new int3(0, 0, 0),
        end = new int3(2, 0, 2),
        entityCost = new EntityConfig(P, entityGrid.AsReadOnly(), OverlapCost)
      };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 2 * PathFinder.CostDiagonal);
    }

    //  0 0 0 0 +
    //  0 + + + 0
    //  + M E 0 0
    //  + M M 0 0
    //  P 0 0 0 0
    [Test]
    public void PathNearEntityWithModifiers()
    {
      const int M = 25;
      using NativeArray<int> entityGrid = new(Length, Allocator.TempJob);
      using PathFinderData data = CreatePathFinderData();
      using MockConfig config = new(Size);
      GridHelper.SetGrid(config.Array, new short[,]
      {
        { 0, 0, 0, 0, 0, },
        { 0, 0, 0, 0, 0, },
        { 0, M, E, 0, 0, },
        { 0, M, M, 0, 0, },
        { 0, 0, 0, 0, 0, }
      });
      GridHelper.SetGrid(entityGrid, new[,]
      {
        { O, O, O, O, O },
        { O, O, O, O, O },
        { O, O, E, O, O },
        { O, O, O, O, O },
        { P, O, O, O, O }
      });
      PathRequest request = new()
      {
        start = new int3(0, 0, 0),
        end = new int3(4, 0, 4),
        entityCost = new EntityConfig(P, entityGrid.AsReadOnly(), OverlapCost)
      };
      Path path = data.pathFinder.FindPath(request);
      GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
      GridHelper.ValidateOptimalPath(in request, path, expected: 4 * PathFinder.CostCardinal + 2 * PathFinder.CostDiagonal);
    }

    [UnityTest]
    public IEnumerator ScheduleJob()
    {
      const int M = 25;
      using NativeArray<int> entityGrid = new(Length, Allocator.Persistent);
      using PathFinderData data = CreatePathFinderData();
      using MockConfig config = new(Size);
      
      GameObject go = new("Test Dispatcher");
      try
      {
        go.AddComponent<JobDispatchManager>();
        GridHelper.SetGrid(config.Array, new short[,]
        {
          { 0, 0, 0, 0, 0, },
          { 0, 0, 0, 0, 0, },
          { 0, M, E, 0, 0, },
          { 0, M, M, 0, 0, },
          { 0, 0, 0, 0, 0, }
        });
        GridHelper.SetGrid(entityGrid, new[,]
        {
          { O, O, O, O, O },
          { O, O, O, O, O },
          { O, O, E, O, O },
          { O, O, O, O, O },
          { P, O, O, O, O }
        });
        PathRequest request = new()
        {
          start = new int3(0, 0, 0),
          end = new int3(4, 0, 4),
          entityCost = new EntityConfig(P, entityGrid.AsReadOnly(), OverlapCost)
        };
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

    private class MockConfig : PathOffsetGrid
    {
      public MockConfig(int2 mapSize) : base(mapSize)
      {
      }

      public NativeArray<short> Array => array;
    }
  }
}