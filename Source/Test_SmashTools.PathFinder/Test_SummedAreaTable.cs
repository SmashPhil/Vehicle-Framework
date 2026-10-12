using DevTools.Testing;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_SummedAreaTable
{
  private const int Width = 5;
  private const int Height = 5;

  private static readonly int2 size = new(5, 5);

  private static readonly SumAreaTable.Layout[] layouts =
  {
      SumAreaTable.Layout.Cardinal, SumAreaTable.Layout.Diagonal
    };

  private static NativeArray<int> GetTestGrid()
  {
    NativeArray<int> pathGrid = new(size.x * size.y, Allocator.TempJob);
    for (int i = 0; i < size.x * size.y; i++)
    {
      pathGrid[i] = i + 1;
    }
    return pathGrid;
  }

  [TestCase(SumAreaTable.Layout.Cardinal, ExpectedResult = (Width + 1) * (Height + 1))]
  [TestCase(SumAreaTable.Layout.Diagonal, ExpectedResult = (Width + Height) * (Width + Height))]
  public int Size(SumAreaTable.Layout layout)
  {
    using NativeArray<int> pathGrid = GetTestGrid();
    using SumAreaTable table = new(new SumAreaTable.Config
    {
      size = size,
      tableCosts = pathGrid.AsReadOnly(),
      layout = layout
    }, Allocator.Temp);
    return table.Table.Length;
  }

  [Test]
  public void EnsureCompleted([ParametersSource(nameof(layouts))] SumAreaTable.Layout layout)
  {
    using NativeArray<int> pathGrid = GetTestGrid();
    using SumAreaTable table = new(new SumAreaTable.Config
    {
      size = size,
      tableCosts = pathGrid.AsReadOnly(),
      layout = layout
    }, Allocator.TempJob);

    table.ScheduleRecalculate();
    table.EnsureCompleted();
    Assert.IsTrue(table.CurrentJob.IsCompleted);
  }

  [Test]
  public void CompleteBeforeDisposal([ParametersSource(nameof(layouts))] SumAreaTable.Layout layout)
  {
    using NativeArray<int> pathGrid = GetTestGrid();

    SumAreaTable table = new(new SumAreaTable.Config
    {
      size = size,
      tableCosts = pathGrid.AsReadOnly(),
      layout = layout
    }, Allocator.TempJob);
    try
    {
      table.ScheduleRecalculate();
      table.Dispose();
      Assert.IsTrue(table.CurrentJob.IsCompleted);
    }
    finally
    {
      table.Dispose();
    }
  }

  [Test]
  public void TabulationCardinal()
  {
    using NativeArray<int> pathGrid = GetTestGrid();
    using SumAreaTable table = new(new SumAreaTable.Config
    {
      size = size,
      tableCosts = pathGrid.AsReadOnly(),
      layout = SumAreaTable.Layout.Cardinal
    }, Allocator.TempJob);
    table.Recalculate();

    int[,] expected =
    {
        { 0,  0,   0,   0,   0,   0 },
        { 0,  1,   3,   6,  10,  15 },
        { 0,  7,  16,  27,  40,  55 },
        { 0, 18,  39,  63,  90, 120 },
        { 0, 34,  72, 114, 160, 210 },
        { 0, 55, 115, 180, 250, 325 }
      };
    for (int y = 0; y < expected.GetLength(0); y++)
    {
      for (int x = 0; x < expected.GetLength(1); x++)
      {
        Assert.AreEqual(expected[y, x], table.Table[x + y * (size.x + 1)]);
      }
    }
  }

  [Test]
  public void TabulationDiagonal()
  {
    using NativeArray<int> pathGrid = GetTestGrid();
    using SumAreaTable table = new(new SumAreaTable.Config
    {
      size = size,
      tableCosts = pathGrid.AsReadOnly(),
      layout = SumAreaTable.Layout.Diagonal
    }, Allocator.TempJob);
    table.Recalculate();

    int[,] expected =
    {
        { 0,  0,  0,   0,   0,   0,   0,   0,   0,   0 },
        { 0,  0,  0,   0,   0,   1,   1,   1,   1,   1 },
        { 0,  0,  0,   0,   6,   7,   9,   9,   9,   9 },
        { 0,  0,  0,  11,  17,  25,  27,  30,  30,  30 },
        { 0,  0, 16,  27,  45,  53,  63,  66,  70,  70 },
        { 0, 21, 37,  65,  83, 104, 114, 126, 130, 135 },
        { 0, 21, 59,  87, 123, 144, 168, 180, 194, 199 },
        { 0, 21, 59, 110, 146, 186, 210, 237, 251, 256 },
        { 0, 21, 59, 110, 170, 210, 254, 281, 295, 300 },
        { 0, 21, 59, 110, 170, 235, 279, 306, 320, 325 }
      };
    for (int y = 0; y < expected.GetLength(0); y++)
    {
      for (int x = 0; x < expected.GetLength(1); x++)
      {
        Assert.AreEqual(expected[y, x], table.Table[x + y * table.Stride]);
      }
    }
  }

  [Test]
  public void SingleCellBlocked()
  {
    const int X0 = 2;
    const int Y0 = 2;

    NativeArray<int> pathGrid = GetTestGrid();
    pathGrid[Y0 * size.x + X0] = PathFinder.ImpassableCost;

    using (pathGrid)
    {
      using SumAreaTable table = new(new SumAreaTable.Config
      {
        size = size,
        tableCosts = pathGrid.AsReadOnly(),
        layout = SumAreaTable.Layout.Cardinal
      }, Allocator.TempJob);
      table.Recalculate();
      Assert.IsTrue(table.IsBlocked(X0, Y0, X0, Y0));
      Expect.GreaterThan(table.GetBlockedSum(X0, Y0, X0, Y0), 0);
    }
  }

  [Test]
  public void MultiCellBlocked()
  {
    const int Xn0 = 0;
    const int Yn0 = 0;
    const int Xn1 = 1;
    const int Yn1 = 1;
    const int X0 = 2;
    const int Y0 = 2;
    const int X1 = 3;
    const int Y1 = 3;
    NativeArray<int> pathGrid = GetTestGrid();
    pathGrid[Y0 * size.x + X0] = PathFinder.ImpassableCost;
    pathGrid[Y0 * size.x + X1] = PathFinder.ImpassableCost;
    pathGrid[Y1 * size.x + X0] = PathFinder.ImpassableCost;
    pathGrid[Y1 * size.x + X1] = PathFinder.ImpassableCost;

    using (pathGrid)
    {
      using SumAreaTable table = new(new SumAreaTable.Config
      {
        size = size,
        tableCosts = pathGrid.AsReadOnly(),
        layout = SumAreaTable.Layout.Cardinal
      }, Allocator.TempJob);
      table.Recalculate();

      Assert.IsTrue(table.IsBlocked(X0, Y0, X1, Y1));
      Expect.GreaterThan(table.GetBlockedSum(X0, Y0, X1, Y1), 0);
      Assert.IsFalse(table.IsBlocked(Xn0, Yn0, Xn1, Yn1));
      Assert.AreEqual(expected: 0, table.GetBlockedSum(Xn0, Yn0, Xn1, Yn1));
    }
  }

  [Test]
  public void MultiCellSingleBlocked()
  {
    const int X0 = 2;
    const int Y0 = 2;
    const int X1 = 3;
    const int Y1 = 3;
    const int Xp = 1;
    const int Yp = 1;
    NativeArray<int> pathGrid = GetTestGrid();
    pathGrid[Y0 * size.x + X0] = PathFinder.ImpassableCost;
    pathGrid[Y0 * size.x + X1] = PathFinder.ImpassableCost;
    pathGrid[Y1 * size.x + X0] = PathFinder.ImpassableCost;
    pathGrid[Y1 * size.x + X1] = PathFinder.ImpassableCost;

    using (pathGrid)
    {
      using SumAreaTable table = new(new SumAreaTable.Config
      {
        size = size,
        tableCosts = pathGrid.AsReadOnly(),
        layout = SumAreaTable.Layout.Cardinal
      }, Allocator.TempJob);
      table.Recalculate();
      Assert.IsTrue(table.IsBlocked(Xp, Yp, X0, Y0));
      Assert.AreEqual(expected: 1, table.GetBlockedSum(Xp, Yp, X0, Y0));
    }
  }

  [Test]
  public void NoBlocked()
  {
    const int Xn0 = 0;
    const int Yn0 = 0;
    const int Xn1 = 1;
    const int Yn1 = 1;
    const int X0 = 2;
    const int Y0 = 2;
    const int X1 = 3;
    const int Y1 = 3;
    NativeArray<int> pathGrid = GetTestGrid();
    pathGrid[Y0 * size.x + X0] = PathFinder.ImpassableCost;
    pathGrid[Y0 * size.x + X1] = PathFinder.ImpassableCost;
    pathGrid[Y1 * size.x + X0] = PathFinder.ImpassableCost;
    pathGrid[Y1 * size.x + X1] = PathFinder.ImpassableCost;

    using (pathGrid)
    {
      using SumAreaTable table = new(new SumAreaTable.Config
      {
        size = size,
        tableCosts = pathGrid.AsReadOnly(),
        layout = SumAreaTable.Layout.Cardinal
      }, Allocator.TempJob);
      table.Recalculate();
      Assert.IsFalse(table.IsBlocked(Xn0, Yn0, Xn1, Yn1));
      Assert.AreEqual(expected: 0, table.GetBlockedSum(Xn0, Yn0, Xn1, Yn1));
    }
  }
}
