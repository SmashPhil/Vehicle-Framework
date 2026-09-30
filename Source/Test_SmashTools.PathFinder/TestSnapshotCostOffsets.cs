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
  internal class TestSnapshotCostOffsets
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

    [Test, Order(0)]
    [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
    public void IndexerOutOfRange()
    {
      const int Offset = 5;

      using MockOffsetGrid config = new(MapSize, Offset);
      Assert.Throws<IndexOutOfRangeException>(() => _ = config[-1]);
      Assert.Throws<IndexOutOfRangeException>(() => _ = config[Size]);
      Assert.Throws<IndexOutOfRangeException>(() => config[-1] = 1);
      Assert.Throws<IndexOutOfRangeException>(() => config[Size] = 1);
    }

    [Test]
    public void DoubleDelete()
    {
      MockOffsetGrid config = new(MapSize, 0);
      Assert.IsTrue(config.ArrayUnsafe.IsCreated);
      config.Dispose();
      Assert.IsFalse(config.ArrayUnsafe.IsCreated);
      Assert.DoesNotThrow(() => config.Dispose());
      Assert.IsFalse(config.ArrayUnsafe.IsCreated);
    }

    [Test]
    public void SnapshotConfigToGrid()
    {
      NativeArray<int> pathGrid = new(Size, Allocator.TempJob);
      NativeArray<int> snapshot = new(Size, Allocator.TempJob);

      const int Value = 1;
      const int Offset = 5;
      using MockOffsetGrid config = new(MapSize, Offset);
      NativeArrayList<short> packedList = PackArrayList(config);
      try
      {
        for (int i = 0; i < Size; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
        }
        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotOffsetsJob(snapshot, packedList).Run(pathGrid.Length);

        for (int i = 0; i < Size; i++)
        {
          Assert.AreEqual(Value + Offset, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
        packedList.Dispose();
      }
    }

    [Test]
    public void SnapshotConfigWithModifiers()
    {
      NativeArray<int> pathGrid = new(Size, Allocator.TempJob);
      NativeArray<int> snapshot = new(Size, Allocator.TempJob);
      using ModifierGrid modifierGrid = new(Size);

      const int Value = 1;
      const int Offset = 5;
      const int Modifier = 2;

      using MockOffsetGrid config = new(MapSize, Offset);
      NativeArrayList<short> packedList = PackArrayList(config);
      try
      {
        const string ModifierName = "UnitTest";
        for (int i = 0; i < Size; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
          modifierGrid.AddModifier(ModifierName, i, new Modifier
          {
            type = ModifierType.Multiply,
            value = Modifier
          }, priority: 0);
        }
        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotOffsetsJob(snapshot, packedList).Run(pathGrid.Length);
        SnapshotModifiersJob modJob = new(snapshot, modifierGrid.InnerGrid);
        modJob.Run(pathGrid.Length);

        for (int i = 0; i < Size; i++)
        {
          Assert.AreEqual(expected: (Value + Offset) * Modifier, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
        packedList.Dispose();
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