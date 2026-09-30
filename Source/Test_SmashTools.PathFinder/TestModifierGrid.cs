using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestModifierGrid
  {
    private const string ModifierName = "UnitTest";

    private static readonly int2 size = new(5, 5);

    [Test, Order(0)]
    public void Constructor()
    {
      int length = size.x * size.y;
      using ModifierGrid grid = new(length);
      Assert.AreEqual(expected: length, grid.InnerGrid.Length);
    }

    [Test]
    public void NotificationOnAddModifier()
    {
      MockNotifier sync = new();
      using ModifierGrid grid = new(size.x * size.y, sync);
      Assert.IsFalse(sync.notified);
      grid.AddModifier(ModifierName, index: 2, new Modifier { type = ModifierType.None });
      Assert.IsTrue(sync.notified);
    }

    [Test]
    public void NoNotificationOnGetModifier()
    {
      MockNotifier sync = new();
      using ModifierGrid grid = new(size.x * size.y, sync);
      Assert.IsFalse(sync.notified);
      _ = grid.GetModifier(index: 0);
      Assert.IsFalse(sync.notified);
    }

    [Test]
    public void CopyWithNoModifiers()
    {
      int length = size.x * size.y;
      NativeArray<int> pathGrid = new(length, Allocator.TempJob);
      NativeArray<int> snapshot = new(length, Allocator.TempJob);

      try
      {
        for (int i = 0; i < length; i++)
        {
          pathGrid[i] = 5;
          snapshot[i] = 1;
        }

        SnapshotGridJob job = new(pathGrid.AsReadOnly(), snapshot);
        job.Run();

        for (int i = 0; i < length; i++)
        {
          Assert.AreEqual(pathGrid[i], snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void ApplySetModifier()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;
      NativeArray<int> pathGrid = new(len, Allocator.TempJob);
      NativeArray<int> snapshot = new(len, Allocator.TempJob);
      using ModifierGrid grid = new(len);

      try
      {
        const int Value = 10;
        const int Modifier = 5;

        for (int i = 0; i < len; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
          grid.AddModifier(ModifierName, i, new Modifier
          {
            type = ModifierType.Add,
            value = Modifier
          }, priority: 0);
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotModifiersJob(snapshot, grid.InnerGrid).Run(pathGrid.Length);

        for (int i = 0; i < len; i++)
        {
          Assert.AreEqual(Value + Modifier, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void ClampSetModifier()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;
      NativeArray<int> pathGrid = new(len, Allocator.TempJob);
      NativeArray<int> snapshot = new(len, Allocator.TempJob);
      using ModifierGrid grid = new(len);

      try
      {
        const int Value = 100;
        const int Modifier = PathFinder.ImpassableCost;

        for (int i = 0; i < len; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
          grid.AddModifier(ModifierName, i, new Modifier
          {
            type = ModifierType.Add,
            value = Modifier
          }, priority: 0);
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotModifiersJob(snapshot, grid.InnerGrid).Run(pathGrid.Length);

        for (int i = 0; i < len; i++)
        {
          Assert.AreEqual(PathFinder.ImpassableCost - 1, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void ApplyAddModifier()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;
      NativeArray<int> pathGrid = new(len, Allocator.TempJob);
      NativeArray<int> snapshot = new(len, Allocator.TempJob);
      using ModifierGrid grid = new(len);

      try
      {
        const int Value = 10;
        const int Modifier = 5;

        for (int i = 0; i < len; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
          grid.AddModifier(ModifierName, i, new Modifier
          {
            type = ModifierType.Add,
            value = Modifier
          }, priority: 0);
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotModifiersJob(snapshot, grid.InnerGrid).Run(pathGrid.Length);

        for (int i = 0; i < len; i++)
        {
          Assert.AreEqual(Value + Modifier, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void ClampAddModifier()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;
      NativeArray<int> pathGrid = new(len, Allocator.TempJob);
      NativeArray<int> snapshot = new(len, Allocator.TempJob);
      using ModifierGrid grid = new(len);

      try
      {
        const int Value = 100;
        const int Modifier = PathFinder.ImpassableCost;

        for (int i = 0; i < len; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
          grid.AddModifier(ModifierName, i, new Modifier
          {
            type = ModifierType.Add,
            value = Modifier
          }, priority: 0);
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotModifiersJob(snapshot, grid.InnerGrid).Run(pathGrid.Length);

        for (int i = 0; i < len; i++)
        {
          Assert.AreEqual(PathFinder.ImpassableCost - 1, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void ApplySubtractModifier()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;
      NativeArray<int> pathGrid = new(len, Allocator.TempJob);
      NativeArray<int> snapshot = new(len, Allocator.TempJob);
      using ModifierGrid grid = new(len);

      try
      {
        const int Value = 10;
        const int Modifier = 5;

        for (int i = 0; i < len; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
          grid.AddModifier(ModifierName, i, new Modifier
          {
            type = ModifierType.Subtract,
            value = Modifier
          }, priority: 0);
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotModifiersJob(snapshot, grid.InnerGrid).Run(pathGrid.Length);

        for (int i = 0; i < len; i++)
        {
          Assert.AreEqual(Value - Modifier, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void ClampSubtractModifier()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;
      NativeArray<int> pathGrid = new(len, Allocator.TempJob);
      NativeArray<int> snapshot = new(len, Allocator.TempJob);
      using ModifierGrid grid = new(len);

      try
      {
        const int Value = 5;
        const int Modifier = 10;

        for (int i = 0; i < len; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
          grid.AddModifier(ModifierName, i, new Modifier
          {
            type = ModifierType.Subtract,
            value = Modifier
          }, priority: 0);
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotModifiersJob(snapshot, grid.InnerGrid).Run(pathGrid.Length);

        for (int i = 0; i < len; i++)
        {
          Assert.AreEqual(expected: 0, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void ApplyMultiplyModifier()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;
      NativeArray<int> pathGrid = new(len, Allocator.TempJob);
      NativeArray<int> snapshot = new(len, Allocator.TempJob);
      using ModifierGrid grid = new(len);

      try
      {
        const int Value = 10;
        const int Modifier = 5;

        for (int i = 0; i < len; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
          grid.AddModifier(ModifierName, i, new Modifier
          {
            type = ModifierType.Multiply,
            value = Modifier
          }, priority: 0);
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotModifiersJob(snapshot, grid.InnerGrid).Run(pathGrid.Length);

        for (int i = 0; i < len; i++)
        {
          Assert.AreEqual(Value * Modifier, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void ClampMultiplyModifier()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;
      NativeArray<int> pathGrid = new(len, Allocator.TempJob);
      NativeArray<int> snapshot = new(len, Allocator.TempJob);
      using ModifierGrid grid = new(len);

      try
      {
        const int Value = 100;
        const int Modifier = PathFinder.ImpassableCost;

        for (int i = 0; i < len; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
          grid.AddModifier(ModifierName, i, new Modifier
          {
            type = ModifierType.Multiply,
            value = Modifier
          }, priority: 0);
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotModifiersJob(snapshot, grid.InnerGrid).Run(pathGrid.Length);

        for (int i = 0; i < len; i++)
        {
          Assert.AreEqual(PathFinder.ImpassableCost - 1, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void ApplyDivideModifier()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;
      NativeArray<int> pathGrid = new(len, Allocator.TempJob);
      NativeArray<int> snapshot = new(len, Allocator.TempJob);
      using ModifierGrid grid = new(len);

      try
      {
        const int Value = 10;
        const int Modifier = 5;

        for (int i = 0; i < len; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
          grid.AddModifier(ModifierName, i, new Modifier
          {
            type = ModifierType.Divide,
            value = Modifier
          }, priority: 0);
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotModifiersJob(snapshot, grid.InnerGrid).Run(pathGrid.Length);

        for (int i = 0; i < len; i++)
        {
          Assert.AreEqual(Value / Modifier, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void ApplyShiftLeftModifier()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;
      NativeArray<int> pathGrid = new(len, Allocator.TempJob);
      NativeArray<int> snapshot = new(len, Allocator.TempJob);
      using ModifierGrid grid = new(len);

      try
      {
        const int Value = 5;
        const int Modifier = 1;

        for (int i = 0; i < len; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
          grid.AddModifier(ModifierName, i, new Modifier
          {
            type = ModifierType.ShiftLeft,
            value = Modifier
          }, priority: 0);
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotModifiersJob(snapshot, grid.InnerGrid).Run(pathGrid.Length);

        for (int i = 0; i < len; i++)
        {
          Assert.AreEqual(Value << Modifier, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void ApplyShiftRightModifier()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;
      NativeArray<int> pathGrid = new(len, Allocator.TempJob);
      NativeArray<int> snapshot = new(len, Allocator.TempJob);
      using ModifierGrid grid = new(len);

      try
      {
        const int Value = 5;
        const int Modifier = 1;

        for (int i = 0; i < len; i++)
        {
          pathGrid[i] = Value;
          snapshot[i] = 0;
          grid.AddModifier(ModifierName, i, new Modifier
          {
            type = ModifierType.ShiftRight,
            value = Modifier
          }, priority: 0);
        }

        new SnapshotGridJob(pathGrid.AsReadOnly(), snapshot).Run();
        new SnapshotModifiersJob(snapshot, grid.InnerGrid).Run(pathGrid.Length);

        for (int i = 0; i < len; i++)
        {
          Assert.AreEqual(Value >> Modifier, snapshot[i]);
        }
      }
      finally
      {
        pathGrid.Dispose();
        snapshot.Dispose();
      }
    }

    [Test]
    public void AddModifierPriority()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;

      using ModifierGrid grid = new(len);

      const string ModifierLow = "LowPriority";
      const ModifierType ModTypeLow = ModifierType.Multiply;
      const ushort ValueLow = 5;

      grid.AddModifier(ModifierLow, index: 0, new Modifier
      {
        type = ModTypeLow,
        value = ValueLow
      }, priority: 2);
      Modifier modifier = grid.GetModifier(index: 0);
      Assert.AreEqual(expected: ModTypeLow, modifier.type);
      Assert.AreEqual(expected: ValueLow, modifier.value);

      const string ModifierHigh = "HighPriority";
      const ModifierType ModTypeHigh = ModifierType.ShiftLeft;
      const ushort ValueHigh = 1;
      grid.AddModifier(ModifierHigh, index: 0, new Modifier
      {
        type = ModTypeHigh,
        value = ValueHigh
      }, priority: 1);
      modifier = grid.GetModifier(index: 0);
      Assert.AreEqual(expected: ModTypeHigh, modifier.type);
      Assert.AreEqual(expected: ValueHigh, modifier.value);
    }

    [Test]
    public void RemoveModifierPriority()
    {
      int width = size.x;
      int height = size.y;
      int len = width * height;

      using ModifierGrid grid = new(len);

      const string ModifierLow = "LowPriority";
      const ModifierType ModTypeLow = ModifierType.Multiply;
      const ushort ValueLow = 5;
      grid.AddModifier(ModifierLow, index: 0, new Modifier
      {
        type = ModTypeLow,
        value = ValueLow
      }, priority: 2);

      const string ModifierHigh = "HighPriority";
      const ModifierType ModTypeHigh = ModifierType.ShiftLeft;
      const ushort ValueHigh = 1;
      grid.AddModifier(ModifierHigh, index: 0, new Modifier
      {
        type = ModTypeHigh,
        value = ValueHigh
      }, priority: 1);

      Modifier modifier = grid.GetModifier(index: 0);
      Assert.AreEqual(expected: ModTypeHigh, modifier.type);
      Assert.AreEqual(expected: ValueHigh, modifier.value);
      grid.RemoveModifier(ModifierHigh, index: 0);
      modifier = grid.GetModifier(index: 0);
      Assert.AreEqual(expected: ModTypeLow, modifier.type);
      Assert.AreEqual(expected: ValueLow, modifier.value);
    }

    private class MockNotifier : IPathGridSync
    {
      public bool notified;

      public void NotifyWritingToGrid()
      {
        notified = true;
      }
    }
  }
}