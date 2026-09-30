using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestSnapshotGrid
  {
    private const int Width = 5;
    private const int Height = 5;
    private const int Size = Width * Height;

    private static readonly int2 MapSize = new(Width, Height);

    [Test]
    public void SnapshotToGrid()
    {
      NativeArray<int> pathGrid = new(Size, Allocator.TempJob);
      NativeArray<int> snapshot = new(Size, Allocator.TempJob);

      const int Value = 3;
      try
      {
        for (int i = 0; i < Size; i++)
        {
          pathGrid[i] = Value;
        }
        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();

        for (int i = 0; i < Size; i++)
        {
          Assert.AreEqual(Value, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void SnapshotConfigWithModifiers()
    {
      NativeArray<int> pathGrid = new(Size, Allocator.TempJob);
      NativeArray<int> snapshot = new(Size, Allocator.TempJob);
      try
      {
        const int Base = 1;
        const int Value = 5;
        using MockGridMask mask = new(MapSize);
        for (int i = 0; i < Size; i++)
        {
          if (i % 2 == 0)
          {
            mask[i] = Value;
          }
        }
        for (int i = 0; i < Size; i++)
        {
          pathGrid[i] = Base;
        }
        new SnapshotMaskedGridJob(pathGrid.AsReadOnly(), mask.ArrayUnsafe, snapshot).Run(pathGrid.Length);

        for (int i = 0; i < Size; i++)
        {
          Assert.AreEqual(expected: i % 2 == 0 ? Value : Base, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    private class MockGridMask : PathGridMask
    {
      public MockGridMask(int2 mapSize) : base(mapSize)
      {
      }
    }
  }
}