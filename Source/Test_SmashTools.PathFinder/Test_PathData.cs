using System;
using CoreLib.Collections;
using DevTools.Testing;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_PathData
{
  private static readonly int2 size = new(2, 2);

  private static NativeArray<int> CreatePathGrid(int2 mapSize)
  {
    NativeArray<int> grid = new(mapSize.x * mapSize.y, Allocator.Temp);
    for (int i = 0; i < grid.Length; i++)
    {
      grid[i] = 1;
    }
    return grid;
  }

  [Test]
  public void Dispose()
  {
    using NativeArray<int> grid = CreatePathGrid(size);

    GridData gridData = new() { mapSize = size, pathGrid = grid.AsReadOnly() };
    PathData pathData = new(gridData, Allocator.Temp);
    try
    {
      Assert.IsTrue(pathData.frontier.IsCreated);
      Assert.IsTrue(pathData.gCosts.IsCreated);
      Assert.IsTrue(pathData.parents.IsCreated);
      Assert.IsTrue(pathData.heading.IsCreated);
      Assert.IsTrue(pathData.visited.IsCreated);
    }
    finally
    {
      pathData.Dispose();
    }
    Assert.IsFalse(pathData.frontier.IsCreated);
    Assert.IsFalse(pathData.gCosts.IsCreated);
    Assert.IsFalse(pathData.parents.IsCreated);
    Assert.IsFalse(pathData.heading.IsCreated);
    Assert.IsFalse(pathData.visited.IsCreated);
  }

  [Test]
  public void Constructor()
  {
    using NativeArray<int> grid = CreatePathGrid(size);
    GridData gridData = new() { mapSize = size, pathGrid = grid.AsReadOnly() };
    using PathData pathData = new(gridData, Allocator.Temp);

    Assert.AreEqual(size, pathData.mapSize);
    Assert.AreEqual(size, pathData.mapSize);

    Assert.AreEqual((ushort)1, pathData.openStatus);
    Assert.AreEqual((ushort)2, pathData.closeStatus);

    Assert.AreEqual(PathData.Status.Stale, pathData.NodeStatus(0));
    Assert.AreEqual(PathData.Status.Stale, pathData.NodeStatus(pathData.Size - 1));
  }

  [Test]
  public void SetNodeStatusOpen()
  {
    using NativeArray<int> grid = CreatePathGrid(size);
    GridData gridData = new() { mapSize = size, pathGrid = grid.AsReadOnly() };
    using PathData pathData = new(gridData, Allocator.Temp);

    const int Index = 1;
    pathData.SetNodeStatus(Index, PathData.Status.Open);
    Assert.AreEqual(pathData.openStatus, pathData.visited[Index]);
    Assert.AreEqual(PathData.Status.Open, pathData.NodeStatus(Index));
  }

  [Test]
  public void SetNodeStatusClosed()
  {
    using NativeArray<int> grid = CreatePathGrid(size);
    GridData gridData = new() { mapSize = size, pathGrid = grid.AsReadOnly() };
    using PathData pathData = new(gridData, Allocator.Temp);

    const int Index = 1;
    pathData.SetNodeStatus(Index, PathData.Status.Closed);
    Assert.AreEqual(pathData.closeStatus, pathData.visited[Index]);
    Assert.AreEqual(PathData.Status.Closed, pathData.NodeStatus(Index));
  }

  [Test]
  public void SetNodeStatusStale()
  {
    using NativeArray<int> grid = CreatePathGrid(size);
    GridData gridData = new() { mapSize = size, pathGrid = grid.AsReadOnly() };
    using PathData pathData = new(gridData, Allocator.Temp);

    const int Index = 1;
    Expect.Throws<InvalidOperationException>(() => pathData.SetNodeStatus(Index, PathData.Status.Stale));
  }

  [Test]
  public void SetNodeStatusInvalid()
  {
    using NativeArray<int> grid = CreatePathGrid(size);
    GridData gridData = new() { mapSize = size, pathGrid = grid.AsReadOnly() };
    using PathData pathData = new(gridData, Allocator.Temp);

    const int Index = 1;
    Expect.Throws<NotImplementedException>(() => pathData.SetNodeStatus(Index, (PathData.Status)1234));
  }

  [Test]
  public void ResetClearsFrontier()
  {
    using NativeArray<int> grid = CreatePathGrid(size);
    GridData gridData = new() { mapSize = size, pathGrid = grid.AsReadOnly() };
    using PathData pathData = new(gridData, Allocator.Temp);
    NativeMinHeap frontier = pathData.frontier;

    frontier.Push(0, 0);
    Assert.AreEqual(expected: 1, frontier.Length);
    pathData.Reset();
    Assert.AreEqual(expected: 0, frontier.Length);
  }

  [Test]
  public void ResetMarksStale()
  {
    using NativeArray<int> grid = CreatePathGrid(size);
    GridData gridData = new() { mapSize = size, pathGrid = grid.AsReadOnly() };
    using PathData pathData = new(gridData, Allocator.Temp);

    const int Index = 1;
    pathData.SetNodeStatus(Index, PathData.Status.Open);
    pathData.Reset();
    Assert.AreEqual(expected: PathData.Status.Stale, pathData.NodeStatus(Index));
  }

  [Test]
  public void ResetBumpsGeneration()
  {
    using NativeArray<int> grid = CreatePathGrid(size);
    GridData gridData = new() { mapSize = size, pathGrid = grid.AsReadOnly() };
    using PathData pathData = new(gridData, Allocator.Temp);

    ushort oldOpen = pathData.openStatus;
    ushort oldClose = pathData.closeStatus;

    pathData.Reset();

    Assert.AreEqual(expected: oldOpen + 2, pathData.openStatus);
    Assert.AreEqual(expected: oldClose + 2, pathData.closeStatus);
  }

  [Test]
  public void ResetFull()
  {
    using NativeArray<int> grid = CreatePathGrid(size);

    GridData gridData = new() { mapSize = size, pathGrid = grid.AsReadOnly() };
    PathData pathData = new(gridData, Allocator.Temp);
    try
    {
      pathData.openStatus = ushort.MaxValue - 2;
      pathData.closeStatus = ushort.MaxValue - 1;

      const int Index = 1;
      pathData.gCosts[Index] = 1;
      pathData.parents[Index] = 1;
      pathData.heading[Index] = 1;
      pathData.visited[Index] = pathData.closeStatus;

      pathData.Reset();
      Assert.AreEqual(expected: 1u, pathData.openStatus);
      Assert.AreEqual(expected: 2u, pathData.closeStatus);
      Assert.AreEqual(expected: 0, pathData.gCosts[Index]);
      Assert.AreEqual(expected: 0, pathData.parents[Index]);
      Assert.AreEqual(expected: 0, pathData.heading[Index]);
      Assert.AreEqual(expected: 0u, pathData.visited[Index]);
    }
    finally
    {
      pathData.Dispose();
    }
  }
}