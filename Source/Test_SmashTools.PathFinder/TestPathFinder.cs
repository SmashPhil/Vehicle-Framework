using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestPathFinder
  {
    private static readonly int2 size = new(5, 5);

    private static NativeArray<int> GetTestGrid()
    {
      NativeArray<int> pathGrid = new(size.x * size.y, Allocator.TempJob);
      for (int i = 0; i < size.x * size.y; i++)
      {
        pathGrid[i] = i + 1;
      }
      return pathGrid;
    }

    [Test]
    public void NullPathGrid()
    {
      Assert.Throws<ArgumentNullException>(delegate
      {
        _ = new PathFinder(new PathFinder.Settings());
      });
    }

    [Test]
    public void NoHitbox()
    {
      Assert.DoesNotThrow(delegate
      {
        using NativeArray<int> pathGrid = GetTestGrid();
        using PathFinder pf = new(new PathFinder.Settings
        {
          pathGrid = pathGrid.AsReadOnly()
        });
      });
    }

    [Test]
    public void SumAreaTable()
    {
      Assert.DoesNotThrow(delegate
      {
        using NativeArray<int> pathGrid = GetTestGrid();
        using PathFinder pf = new(new PathFinder.Settings
        {
          pathGrid = pathGrid.AsReadOnly(),
          hitbox = new int2(2, 2)
        });
      });
    }
  }
}