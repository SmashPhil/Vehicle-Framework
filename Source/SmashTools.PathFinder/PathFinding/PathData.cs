using System;
using CoreLib.Collections;
using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  internal struct GridData
  {
    public int2 mapSize;
    public NativeArray<int>.ReadOnly pathGrid;
    public NativeArray<uint>.ReadOnly indexToCell;
  }

  /// <summary>
  /// Persistent data pooled and shared between jobs.
  /// </summary>
  internal struct PathData : IDisposable
  {
    public readonly int2 mapSize;
    public readonly int stride;

    [ReadOnly]
    public NativeArray<int>.ReadOnly pathGrid;

    [ReadOnly]
    public NativeArray<uint>.ReadOnly indexToCell;

    public NativeFixedList<int> path;
    public NativeMinHeap frontier;

    public NativeArray<int> gCosts;
    public NativeArray<int> parents;
    public NativeArray<int> heading;
    public NativeArray<int> turnCooldown;

    public NativeArray<ushort> visited;

    public ushort openStatus;
    public ushort closeStatus;

    private bool disposed;

    public PathData(in GridData gridData, Allocator allocator)
    {
      mapSize = gridData.mapSize;
      stride = mapSize.x + 1;
      pathGrid = gridData.pathGrid;
      indexToCell = gridData.indexToCell;

      Size = mapSize.x * mapSize.y;
      path = new NativeFixedList<int>(Size, allocator);
      frontier = new NativeMinHeap(Size, allocator);
      gCosts = new NativeArray<int>(Size, allocator);
      parents = new NativeArray<int>(Size, allocator);
      heading = new NativeArray<int>(Size, allocator);
      turnCooldown = new NativeArray<int>(Size, allocator);
      visited = new NativeArray<ushort>(Size, allocator);

      openStatus = 0;
      closeStatus = 0;

      disposed = false;

      ResetStatus();
    }

    public int Size { get; }

    public readonly Status NodeStatus(int index)
    {
      ushort status = visited[index];
      if (status == closeStatus)
        return Status.Closed;

      return status == openStatus ? Status.Open : Status.Stale;
    }

    public void SetNodeStatus(int index, Status status)
    {
      visited[index] = status switch
      {
        Status.Open => openStatus,
        Status.Closed => closeStatus,
        Status.Stale => throw new InvalidOperationException("Invalid status value."),
        _ => throw new NotImplementedException(nameof(Status))
      };
    }

    public void Reset()
    {
      path.Clear();
      frontier.Clear();
      
      // Bump generation
      if (closeStatus == ushort.MaxValue - 1)
      {
        BurstUtils.MemClear(gCosts);
        BurstUtils.MemClear(heading);
        BurstUtils.MemClear(parents);
        BurstUtils.MemClear(turnCooldown);
        BurstUtils.MemClear(visited);
        ResetStatus();
      }
      else
      {
        openStatus += 2;
        closeStatus += 2;
      }
    }

    private void ResetStatus()
    {
      openStatus = 1;
      closeStatus = 2;
    }

    public void Dispose()
    {
      if (disposed)
        return;

      disposed = true;
      path.Dispose();
      frontier.Dispose();
      gCosts.Dispose();
      heading.Dispose();
      parents.Dispose();
      turnCooldown.Dispose();
      visited.Dispose();
    }

    public struct Offset
    {
      public int4 xIndices;
      public int4 yIndices;
    }

    public enum Status
    {
      Stale,
      Open,
      Closed
    }
  }
}