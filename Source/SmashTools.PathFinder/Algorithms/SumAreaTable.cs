using System;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  /// <summary>
  /// Summed area table implementation for fast rect summations.
  /// </summary>
  /// <remarks><see href="https://en.wikipedia.org/wiki/Summed-area_table"/></remarks>
  [PublicAPI]
  public sealed class SumAreaTable : IDisposable
  {
    private readonly int width;
    private readonly int height;
    private readonly Layout layout;

    private NativeArray<int> table;
    private NativeArray<int> blocked;

    private JobHandle handle;

    private readonly NativeArray<int>.ReadOnly tableCosts;

    public SumAreaTable(in Config config, Allocator allocator = Allocator.Persistent)
    {
      width = config.size.x;
      height = config.size.y;
      layout = config.layout;
      tableCosts = config.tableCosts;

      Stride = layout switch
      {
        // Pad extra row and column for 0 sum edges. This allows us to skip boundary checks during recalcs.
        Layout.Cardinal => width + 1,
        // Diagonal stride is much larger so we can normalize the hitbox to the grid
        Layout.Diagonal => width + height,
        _ => throw new NotImplementedException(layout.ToString())
      };
      Size = layout switch
      {
        Layout.Cardinal => Stride * (height + 1),
        Layout.Diagonal => Stride * Stride,
        _ => throw new NotImplementedException(layout.ToString())
      };
      table = new NativeArray<int>(Size, allocator);
      blocked = new NativeArray<int>(Size, allocator);
    }

    public int Size { get; }

    public int Stride { get; }

    public JobHandle CurrentJob => handle;

    public NativeArray<int>.ReadOnly Table => table.AsReadOnly();

    public NativeArray<int>.ReadOnly BlockTable => blocked.AsReadOnly();

    public bool IsBlocked(int x0, int y0, int x1, int y1)
    {
      return GetBlockedSum(x0, y0, x1, y1) > 0;
    }

    public int GetSum(int x0, int y0, int x1, int y1)
    {
      return GetSum(Table, Stride, x0, y0, x1, y1);
    }

    public int GetBlockedSum(int x0, int y0, int x1, int y1)
    {
      return GetSum(BlockTable, Stride, x0, y0, x1, y1);
    }

    public static int GetSum(in NativeArray<int>.ReadOnly table, int stride, int x0, int y0, int x1, int y1)
    {
      int a = table[y0 * stride + x0];
      int b = table[y0 * stride + x1 + 1];
      int c = table[(y1 + 1) * stride + x0];
      int d = table[(y1 + 1) * stride + x1 + 1];
      return d - c - b + a;
    }

    public void EnsureCompleted()
    {
      handle.Complete();
    }

    public void Recalculate()
    {
      EnsureCompleted();
      if (layout is Layout.Cardinal)
      {
        new SumCardinalJob(width, height, new JobData
        {
          table = table,
          blocked = blocked,
          tableCosts = tableCosts
        }).Run();
      }
      else
      {
        new SumDiagonalJob(width, height, new JobData
        {
          table = table,
          blocked = blocked,
          tableCosts = tableCosts
        }).Run();
      }
    }

    public JobHandle ScheduleRecalculate(JobHandle dependsOn = default)
    {
      EnsureCompleted();
      if (layout is Layout.Cardinal)
      {
        handle = new SumCardinalJob(width, height, new JobData
        {
          table = table,
          blocked = blocked,
          tableCosts = tableCosts
        }).Schedule(dependsOn);
      }
      else
      {
        handle = new SumDiagonalJob(width, height, new JobData
        {
          table = table,
          blocked = blocked,
          tableCosts = tableCosts
        }).Schedule(dependsOn);
      }
      return handle;
    }

    public void Dispose()
    {
      EnsureCompleted();
      if (table.IsCreated)
      {
        table.Dispose();
      }

      if (blocked.IsCreated)
      {
        blocked.Dispose();
      }
    }

    internal struct JobData
    {
      public NativeArray<int> table;
      public NativeArray<int> blocked;
      public NativeArray<int>.ReadOnly tableCosts;
    }

    public struct Config
    {
      public int2 size;
      public NativeArray<int>.ReadOnly tableCosts;
      public Layout layout;
    }

    public enum Layout
    {
      Cardinal,
      Diagonal
    }
  }
}