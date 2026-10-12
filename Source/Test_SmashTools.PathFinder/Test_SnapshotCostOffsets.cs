using System;
using System.Diagnostics;
using CoreLib.Collections;
using DevTools.Testing;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_SnapshotCostOffsets
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

  [Test]
  public void DoubleDelete()
  {
    MockOffsetGrid config = new(MapSize, 0);
    Assert.IsTrue(config.ArrayUnsafe.IsCreated);
    config.Dispose();
    Assert.IsFalse(config.ArrayUnsafe.IsCreated);
    Expect.DoesNotThrow(() => config.Dispose());
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

#if ENABLE_UNITY_COLLECTIONS_CHECKS
  [Test]
  public void IndexerOutOfRange()
  {
    const int Offset = 5;

    using MockOffsetGrid config = new(MapSize, Offset);
    Expect.Throws<IndexOutOfRangeException>(() => _ = config[-1]);
    Expect.Throws<IndexOutOfRangeException>(() => _ = config[Size]);
    Expect.Throws<IndexOutOfRangeException>(() => config[-1] = 1);
    Expect.Throws<IndexOutOfRangeException>(() => config[Size] = 1);
  }
#endif

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
