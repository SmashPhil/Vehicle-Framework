using System;
using System.Diagnostics;
using NUnit.Framework;
using SmashTools.PathFinder.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestSnapshotBitOffsets
  {
    private const int Width = 5;
    private const int Height = 5;
    private const int Size = Width * Height;

    private static readonly int2 MapSize = new(Width, Height);

    private static NativeArrayList<short> PackArrayList(MockOffsetGrid config)
    {
      NativeArray<short>.ReadOnly[] array = { config.ArrayUnsafe };
      return NativeArrayList<short>.PackArrayList(array, Allocator.TempJob);
    }

    private static SnapshotBitOffsetsJob.Data PackBitOffsets(MockBoolGrid grid, Allocator allocator = Allocator.TempJob)
    {
      NativeBitArraySlim.ReadOnly[] array = { grid.ArrayUnsafe };
      NativeArray<short> costSet = new(length: 1, allocator);
      costSet[0] = grid.cost.set;
      NativeArray<short> costUnset = new(length: 1, allocator);
      costUnset[0] = grid.cost.unset;
      return new SnapshotBitOffsetsJob.Data
      {
        bitArrays = NativeArrayList<ulong>.PackBitArrayList(array, Allocator.TempJob),
        offsetSet = costSet,
        offsetUnset = costUnset
      };
    }

    [Test, Order(0)]
    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    public void IndexerOutOfRange()
    {
      const int Value = 5;

      using MockBoolGrid grid = new(MapSize, set: true, Value);
      Assert.Throws<IndexOutOfRangeException>(() => _ = grid[Size]);
      Assert.Throws<IndexOutOfRangeException>(() => grid[Size] = true);
    }

    [Test]
    public void DoubleDelete()
    {
      MockBoolGrid grid = new(MapSize, set: true, value: 0);
      Assert.IsTrue(grid.ArrayUnsafe.IsCreated);
      grid.Dispose();
      Assert.IsFalse(grid.ArrayUnsafe.IsCreated);
      Assert.DoesNotThrow(() => grid.Dispose());
      Assert.IsFalse(grid.ArrayUnsafe.IsCreated);
    }

    [Test]
    public void SnapshotBitOffsetsSet()
    {
      NativeArray<int> pathGrid = new(Size, Allocator.TempJob);
      NativeArray<int> snapshot = new(Size, Allocator.TempJob);

      const bool Set = true;
      const int Value = 5;
      using MockBoolGrid grid = new(MapSize, Set, Value);
      using SnapshotBitOffsetsJob.Data data = PackBitOffsets(grid);
      try
      {
        const int PathCost = 1;
        for (int i = 0; i < Size; i++)
        {
          pathGrid[i] = PathCost;
          snapshot[i] = 0;
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotBitOffsetsJob(snapshot, data).Run(pathGrid.Length);

        for (int i = 0; i < Size; i++)
        {
          Assert.AreEqual(PathCost + Value, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void SnapshotBitOffsetsUnset()
    {
      NativeArray<int> pathGrid = new(Size, Allocator.TempJob);
      NativeArray<int> snapshot = new(Size, Allocator.TempJob);

      const bool Set = false;
      const int Value = 5;
      using MockBoolGrid grid = new(MapSize, Set, Value);
      using SnapshotBitOffsetsJob.Data data = PackBitOffsets(grid);
      try
      {
        const int PathCost = 1;
        for (int i = 0; i < Size; i++)
        {
          pathGrid[i] = PathCost;
          snapshot[i] = 0;
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotBitOffsetsJob(snapshot, data).Run(pathGrid.Length);

        for (int i = 0; i < Size; i++)
        {
          Assert.AreEqual(PathCost, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void SnapshotConfigWithBitOffsets()
    {
      NativeArray<int> pathGrid = new(Size, Allocator.TempJob);
      NativeArray<int> snapshot = new(Size, Allocator.TempJob);

      const bool Set = true;
      const int Value = 5;
      const int Offset = 3;
      using MockBoolGrid grid = new(MapSize, Set, Value);
      using MockOffsetGrid offsets = new(MapSize, Offset);
      using SnapshotBitOffsetsJob.Data data = PackBitOffsets(grid);
      using NativeArrayList<short> offsetList = PackArrayList(offsets);
      try
      {
        const int PathCost = 1;
        for (int i = 0; i < Size; i++)
        {
          pathGrid[i] = PathCost;
          snapshot[i] = 0;
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotOffsetsJob(snapshot, offsetList).Run(pathGrid.Length);
        new SnapshotBitOffsetsJob(snapshot, data).Run(pathGrid.Length);

        for (int i = 0; i < Size; i++)
        {
          Assert.AreEqual(PathCost + Offset + Value, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    private class MockBoolGrid : PathBoolGrid
    {
      public readonly Cost cost;

      public MockBoolGrid(int2 mapSize, bool set, short value) : base(mapSize)
      {
        cost = new Cost { set = value };
        Populate(set);
      }

      private void Populate(bool set)
      {
        for (int i = 0; i < Length; i++)
        {
          this[i] = set;
        }
      }
    }

    private class MockOffsetGrid : PathOffsetGrid
    {
      public MockOffsetGrid(int2 mapSize, short value) : base(mapSize)
      {
        Populate(value);
      }

      private void Populate(short value)
      {
        for (int i = 0; i < Length; i++)
        {
          this[i] = value;
        }
      }
    }
  }
}