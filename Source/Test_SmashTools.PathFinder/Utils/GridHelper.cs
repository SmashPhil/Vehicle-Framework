
using System.Text;
using CoreLib.Collections;
using CoreLib.PathFinding;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder.Tests;

internal static class GridHelper
{
  internal static void SetDefaultValues(int[,] grid)
  {
    for (int i = 0; i < grid.GetLength(0); i++)
    {
      for (int j = 0; j < grid.GetLength(1); j++)
      {
        grid[i, j] = 1;
      }
    }
  }

  internal static void SetDefaultValues<T>(NativeArray<T> native, T value) where T : unmanaged
  {
    for (int i = 0; i < native.Length; i++)
    {
      native[i] = value;
    }
  }

  internal static void SetGrid<T>(NativeArray<T> native, T[,] grid) where T : struct
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
        T cost = grid[i, x];
        native[index] = cost;
      }
    }
  }

  internal static void SetBitArray(NativeBitArraySlim bitArray, int[,] grid)
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
        bool value = grid[i, x] != 0;
        bitArray.Set(index, value);
      }
    }
  }

  internal static void ValidatePathResult(NativeArray<int>.ReadOnly snapshot, int2 mapSize, in PathRequest req, Path path)
  {
    Assert.IsTrue(path.IsValid);
    Assert.AreEqual(req.start, path.FirstNode.ToInt3());
    Assert.AreEqual(req.end, path.LastNode.ToInt3());

    int3 prev = req.start;
    while (!path.IsFinished)
    {
      int3 current = path.ConsumeNextNode().ToInt3();
      Assert.IsTrue(InBounds(current), $"{current} is out of bounds.");
      Assert.IsTrue(IsPassable(current), $"{current} is not passable.");

      // Neighbors
      int dx = current.x - prev.x;
      int dz = current.z - prev.z;
      int adx = math.abs(dx);
      int adz = math.abs(dz);
      bool cardinal = (adx == 1 && adz == 0) || (adx == 0 && adz == 1);
      bool diagonal = adx == 1 && adz == 1;
      Assert.IsTrue(cardinal || diagonal);

      if (diagonal)
      {
        // Traversal is allowed by passable cardinal neighbors
        int3 sideA = new(current.x, 0, prev.z);
        int3 sideB = new(prev.x, 0, current.z);

        Assert.IsTrue(InBounds(sideA), $"SideA {sideA} is out of bounds.");
        Assert.IsTrue(InBounds(sideB), $"SideB {sideB} is out of bounds.");
        Assert.IsTrue(IsPassable(sideA), $"sideA {sideA} is not passable.");
        Assert.IsTrue(IsPassable(sideB), $"sideB {sideB} is not passable.");
      }
      prev = current;
    }
    Assert.AreEqual(req.end, prev);
    return;

    bool IsPassable(int3 pos)
    {
      int index = pos.x + pos.z * mapSize.x;
      return snapshot[index] < PathFinder.ImpassableCost;
    }

    bool InBounds(int3 pos)
    {
      return (uint)pos.x < mapSize.x && (uint)pos.z < mapSize.y;
    }
  }

  internal static void ValidateOptimalPath(in PathRequest req, Path path, int expected)
  {
    if (path.IsFinished)
    {
      path.ResetPathToStart();
    }
    Assert.IsFalse(path.IsFinished);
    int3 prev = req.start;
    int cost = 0;
    while (!path.IsFinished)
    {
      int3 current = path.ConsumeNextNode().ToInt3();

      int dx = current.x - prev.x;
      int dz = current.z - prev.z;
      int adx = math.abs(dx);
      int adz = math.abs(dz);
      bool cardinal = (adx == 1 && adz == 0) || (adx == 0 && adz == 1);
      bool diagonal = adx == 1 && adz == 1;
      Assert.IsTrue(cardinal || diagonal);
      cost += cardinal ? PathFinder.CostCardinal : PathFinder.CostDiagonal;
      prev = current;
    }
    Assert.AreEqual(expected, cost);
  }

  internal static int PathCost(in PathRequest req, Path path)
  {
    if (path.IsFinished)
    {
      path.ResetPathToStart();
    }
    Assert.IsFalse(path.IsFinished);
    int3 prev = req.start;
    int cost = 0;
    while (!path.IsFinished)
    {
      int3 current = path.ConsumeNextNode().ToInt3();

      int dx = current.x - prev.x;
      int dz = current.z - prev.z;
      int adx = math.abs(dx);
      int adz = math.abs(dz);
      bool cardinal = (adx == 1 && adz == 0) || (adx == 0 && adz == 1);
      bool diagonal = adx == 1 && adz == 1;
      Assert.IsTrue(cardinal || diagonal);
      cost += cardinal ? PathFinder.CostCardinal : PathFinder.CostDiagonal;
      prev = current;
    }
    return cost;
  }

  internal static void PrintPath(NativeArray<int> pathGrid, int2 mapSize, in PathRequest req, Path path)
  {
    if (path.IsFinished)
    {
      path.ResetPathToStart();
    }
    int width = mapSize.x;
    int height = mapSize.y;

    string[,] display = new string[height, width];

    for (int i = 0; i < height; i++)
    {
      int y = height - 1 - i;
      for (int x = 0; x < width; x++)
      {
        int index = x + y * width;
        int val = pathGrid[index];
        display[i, x] = val >= PathFinder.ImpassableCost ? "X" : val.ToString();
      }
    }

    int startRow = height - 1 - req.start.z;
    int startCol = req.start.x;
    int endRow = height - 1 - req.end.z;
    int endCol = req.end.x;

    display[startRow, startCol] = "s";
    display[endRow, endCol] = "e";

    if (path.IsFinished)
    {
      path.ResetPathToStart();
    }

    while (!path.IsFinished)
    {
      int3 node = path.ConsumeNextNode().ToInt3();
      int row = height - 1 - node.z;
      int col = node.x;

      if (row == startRow && col == startCol)
        continue;
      if (row == endRow && col == endCol)
        continue;

      display[row, col] = "o";
    }

    StringBuilder sb = new();
    for (int i = 0; i < height; i++)
    {
      sb.Append("  ");
      for (int x = 0; x < width; x++)
      {
        sb.Append(display[i, x]);
        if (x < width - 1)
          sb.Append("  ");
      }
      sb.Append("  ");
      if (i < height - 1)
        sb.AppendLine("  ");
    }

    Debug.Log(sb.ToString());
    path.ResetPathToStart();
  }
}
