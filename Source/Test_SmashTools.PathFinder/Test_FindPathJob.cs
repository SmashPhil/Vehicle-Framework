using System.Collections;
using CoreLib.Jobs;
using CoreLib.PathFinding;
using DevTools.Testing;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_FindPathJob
{
  private const int X = PathFinder.ImpassableCost;
  private const int MapWidth = 5;
  private const int MapHeight = 5;

  private static readonly int[,] EmptyGrid;
  private static readonly int2 Size = new(MapWidth, MapHeight);

  private NativeArray<int> pathGrid;
  private PathFinder pathFinder;

  static Test_FindPathJob()
  {
    EmptyGrid = new int[MapWidth, MapHeight];
    GridHelper.SetDefaultValues(EmptyGrid);
  }

  private void SetGrid(int[,] grid)
  {
    GridHelper.SetGrid(pathGrid, grid);
  }

  [SetUp]
  public void InitPathFinding()
  {
    pathGrid = new NativeArray<int>(MapWidth * MapHeight, Allocator.Persistent);
    pathFinder = new PathFinder(new PathFinder.Settings
    {
      mapSize = new int2(MapWidth, MapHeight),
      pathGrid = pathGrid.AsReadOnly(),
      poolObjects = false
    });
  }

  [TearDown]
  public void CleanupPathFinding()
  {
    pathGrid.Dispose();
    pathFinder.Dispose();
    pathFinder = null;
  }

  [Test]
  public void SameCell()
  {
    SetGrid(EmptyGrid);
    PathRequest request = new() { start = new int3(0, 0, 0), end = new int3(0, 0, 0) };
    Path path = pathFinder.FindPath(request);
    Assert.IsFalse(path.IsValid);
    Assert.AreEqual(0, path.NodesLeft);
  }

  [Test]
  public void Horizontal()
  {
    SetGrid(EmptyGrid);
    PathRequest request = new() { start = new int3(1, 0, 2), end = new int3(3, 0, 2) };
    Path path = pathFinder.FindPath(request);
    Assert.IsTrue(path.IsValid);
    Assert.AreEqual(request.start, path.FirstNode.ToInt3());
    Assert.AreEqual(request.end, path.LastNode.ToInt3());
    Assert.AreEqual(expected: 3, path.NodesLeft);
    for (int i = 0; i < path.NodesLeft; i++)
    {
      Assert.AreEqual(new int3(i + 1, 0, 2), path.Peek(i).ToInt3());
    }
  }

  [Test]
  public void Vertical()
  {
    SetGrid(EmptyGrid);
    PathRequest request = new() { start = new int3(2, 0, 1), end = new int3(2, 0, 3) };
    Path path = pathFinder.FindPath(request);
    Assert.IsTrue(path.IsValid);
    Assert.AreEqual(request.start, path.FirstNode.ToInt3());
    Assert.AreEqual(request.end, path.LastNode.ToInt3());
    Assert.AreEqual(expected: 3, path.NodesLeft);
    for (int i = 0; i < path.NodesLeft; i++)
    {
      Assert.AreEqual(new int3(2, 0, i + 1), path.Peek(i).ToInt3());
    }
  }

  [Test]
  public void Diagonal()
  {
    SetGrid(EmptyGrid);
    PathRequest request = new() { start = new int3(1, 0, 1), end = new int3(3, 0, 3) };
    Path path = pathFinder.FindPath(request);
    Assert.IsTrue(path.IsValid);
    Assert.AreEqual(request.start, path.FirstNode.ToInt3());
    Assert.AreEqual(request.end, path.LastNode.ToInt3());
    Assert.AreEqual(expected: 3, path.NodesLeft);
    for (int i = 0; i < path.NodesLeft; i++)
    {
      Assert.AreEqual(new int3(i + 1, 0, i + 1), path.Peek(i).ToInt3());
    }
  }

  [Test]
  public void BackwardsHorizontal()
  {
    SetGrid(EmptyGrid);
    PathRequest request = new() { start = new int3(3, 0, 2), end = new int3(1, 0, 2) };
    Path path = pathFinder.FindPath(request);
    Assert.IsTrue(path.IsValid);
    Assert.AreEqual(request.start, path.FirstNode.ToInt3());
    Assert.AreEqual(request.end, path.LastNode.ToInt3());
    Assert.AreEqual(expected: 3, path.NodesLeft);
    for (int i = 0; i < path.NodesLeft; i++)
    {
      Assert.AreEqual(new int3(3 - i, 0, 2), path.Peek(i).ToInt3());
    }
  }

  [Test]
  public void BackwardsVertical()
  {
    SetGrid(EmptyGrid);
    PathRequest request = new() { start = new int3(2, 0, 3), end = new int3(2, 0, 1) };
    Path path = pathFinder.FindPath(request);
    Assert.IsTrue(path.IsValid);
    Assert.AreEqual(request.start, path.FirstNode.ToInt3());
    Assert.AreEqual(request.end, path.LastNode.ToInt3());
    Assert.AreEqual(expected: 3, path.NodesLeft);
    for (int i = 0; i < path.NodesLeft; i++)
    {
      Assert.AreEqual(new int3(2, 0, 3 - i), path.Peek(i).ToInt3());
    }
  }

  [Test]
  public void BackwardsDiagonal()
  {
    SetGrid(EmptyGrid);
    PathRequest request = new() { start = new int3(3, 0, 1), end = new int3(1, 0, 3) };
    Path path = pathFinder.FindPath(request);
    Assert.IsTrue(path.IsValid);
    Assert.AreEqual(request.start, path.FirstNode.ToInt3());
    Assert.AreEqual(request.end, path.LastNode.ToInt3());
    Assert.AreEqual(expected: 3, path.NodesLeft);
    for (int i = 0; i < path.NodesLeft; i++)
    {
      Assert.AreEqual(new int3(3 - i, 0, i + 1), path.Peek(i).ToInt3());
    }
  }

  [Test]
  public void NoPath()
  {
    SetGrid(new[,]
    {
        { X, X, X, X, X },
        { X, X, X, X, X },
        { X, 1, X, 1, X },
        { X, X, X, X, X },
        { X, X, X, X, X }
      });
    PathRequest request = new() { start = new int3(1, 0, 2), end = new int3(3, 0, 2) };
    Path path = pathFinder.FindPath(request);
    Assert.IsFalse(path.IsValid);
  }

  [Test]
  public void EdgeBoundary()
  {
    SetGrid(new[,]
    {
        { 1, 1, 1, 1, 1 },
        { 1, X, X, X, 1 },
        { 1, X, X, X, 1 },
        { 1, X, X, X, 1 },
        { 1, X, X, X, 1 }
      });
    PathRequest request = new() { start = new int3(0, 0, 0), end = new int3(4, 0, 0) };
    Path path = pathFinder.FindPath(request);
    GridHelper.ValidatePathResult(pathFinder.Snapshot, Size, request, path);
  }

  [Test]
  public void HorizontalWall()
  {
    SetGrid(new[,]
    {
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, X, 1, },
        { 1, 1, 1, X, 1, },
        { 1, 1, 1, X, 1, },
        { 1, 1, 1, X, 1, }
      });
    PathRequest request = new() { start = new int3(0, 0, 0), end = new int3(4, 0, 0) };
    Path path = pathFinder.FindPath(request);
    GridHelper.ValidatePathResult(pathFinder.Snapshot, Size, request, path);
    GridHelper.ValidateOptimalPath(in request, path, expected: 8 * PathFinder.CostCardinal + 2 * PathFinder.CostDiagonal);
  }

  [Test]
  public void VerticalWall()
  {
    SetGrid(new[,]
    {
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { X, X, X, X, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, }
      });
    PathRequest request = new() { start = new int3(0, 0, 0), end = new int3(0, 0, 4) };
    Path path = pathFinder.FindPath(request);
    GridHelper.ValidatePathResult(pathFinder.Snapshot, Size, request, path);
    GridHelper.ValidateOptimalPath(in request, path, expected: 8 * PathFinder.CostCardinal + 2 * PathFinder.CostDiagonal);
  }

  [Test]
  public void DiagonalBlocked()
  {
    SetGrid(new[,]
    {
        { X, X, X, X, X },
        { X, X, X, 1, X },
        { X, X, 1, X, X },
        { X, 1, X, X, X },
        { X, X, X, X, X }
      });
    PathRequest request = new() { start = new int3(1, 0, 1), end = new int3(3, 0, 3) };
    Path path = pathFinder.FindPath(request);
    Assert.IsFalse(path.IsValid);
  }

  [Test]
  public void DiagonalWall()
  {
    SetGrid(new[,]
    {
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, X, 1, },
        { 1, 1, X, 1, 1, },
        { 1, X, 1, 1, 1, },
        { X, 1, 1, 1, 1, }
      });
    PathRequest request = new() { start = new int3(1, 0, 0), end = new int3(0, 0, 1) };
    Path path = pathFinder.FindPath(request);
    GridHelper.ValidatePathResult(pathFinder.Snapshot, Size, request, path);
    GridHelper.ValidateOptimalPath(in request, path, expected: 6 * PathFinder.CostCardinal + 4 * PathFinder.CostDiagonal);
  }

  [Test]
  public void PassableCosts()
  {
    const int B = 50;

    SetGrid(new[,]
    {
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, B, 1, 1, },
        { 1, 1, B, 1, 1, },
        { 1, 1, B, 1, 1, }
      });
    PathRequest request = new() { start = new int3(0, 0, 0), end = new int3(4, 0, 0) };
    Path path = pathFinder.FindPath(request);
    GridHelper.ValidatePathResult(pathFinder.Snapshot, Size, request, path);
    GridHelper.ValidateOptimalPath(in request, path, expected: 2 * PathFinder.CostCardinal + 4 * PathFinder.CostDiagonal);
  }

  [Test]
  public IEnumerator ScheduleJob()
  {
    GameObject go = new("Test Dispatcher");
    go.AddComponent<JobDispatchManager>();
    try
    {
      SetGrid(EmptyGrid);
      PathRequest request = new() { start = new int3(1, 0, 2), end = new int3(3, 0, 2) };
      using PathReceipt receipt = pathFinder.RequestPath(request);

      // Testing to make sure PathFinder + JobDispatchManager both process and complete the request
      // as expected, which is why we shouldn't Complete the job, but rather wait for it to finish.
      while (!receipt.IsCompleted)
      {
        yield return null;
      }

      Path path = receipt.GetPath();
      Assert.IsTrue(path.IsValid);
      GridHelper.ValidatePathResult(pathFinder.Snapshot, Size, request, path);
    }
    finally
    {
      Object.Destroy(go);
    }
  }
}
