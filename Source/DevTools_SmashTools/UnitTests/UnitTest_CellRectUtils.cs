using System.Collections.Generic;
using System.Linq;
using DevTools.UnitTesting;
using UnityEngine.Assertions;
using Verse;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestDescription("Ext_CellRect extension methods.")]
internal class UnitTest_CellRectUtils
{
  [Test]
  private void Cardinals()
  {
    const int MaxSize = 5;

    IntVec3 position = new(10, 0, 10);

    IntVec3 north;
    IntVec3 east;
    IntVec3 south;
    IntVec3 west;

    CellRect cellRect = CellRect.SingleCell(position);
    List<IntVec3> cardinals = cellRect.Cardinals().ToList();
    Expect.AreEqual(cardinals.Count, 1, "Cardinals (1,1) Count");
    Expect.AreEqual(position, cardinals[0], "Cardinals (1,1) Single");

    // 1x2 to 1xN
    for (int z = 2; z <= MaxSize; z++)
    {
      const int x = 1;
      cellRect = CellRect.CenteredOn(position, x, z);

      north = new IntVec3(position.x, 0, cellRect.maxZ);
      south = new IntVec3(position.x, 0, cellRect.minZ);
      cardinals = cellRect.Cardinals().ToList();
      Expect.AreEqual(cardinals.Count, 2, $"Cardinals ({x},{z}) Count");
      Expect.AreEqual(north, cardinals[0], $"Cardinals ({x},{z}) North");
      Expect.AreEqual(south, cardinals[1], $"Cardinals ({x},{z}) South");
    }

    // 2x1 to Nx1
    for (int x = 2; x <= MaxSize; x++)
    {
      const int z = 1;
      cellRect = CellRect.CenteredOn(position, x, z);

      east = new IntVec3(cellRect.maxX, 0, position.z);
      west = new IntVec3(cellRect.minX, 0, position.z);
      cardinals = cellRect.Cardinals().ToList();
      Expect.AreEqual(cardinals.Count, 2, $"Cardinals ({x},{z}) Count");
      Expect.AreEqual(east, cardinals[0], $"Cardinals ({x},{z}) East");
      Expect.AreEqual(west, cardinals[1], $"Cardinals ({x},{z}) West");
    }

    cellRect = CellRect.CenteredOn(position, 2, 2);

    IntVec3 northEast = new(cellRect.maxX, 0, cellRect.maxZ);
    IntVec3 southEast = new(cellRect.maxX, 0, cellRect.minZ);
    IntVec3 southWest = new(cellRect.minX, 0, cellRect.minZ);
    IntVec3 northWest = new(cellRect.minX, 0, cellRect.maxZ);

    // 2x2 special case
    cardinals = cellRect.Cardinals().ToList();
    Expect.AreEqual(cardinals.Count, 4, "Cardinals (2,2) Count");
    Expect.AreEqual(northEast, cardinals[0], "Cardinals (2,2) North");
    Expect.AreEqual(southEast, cardinals[1], "Cardinals (2,2) South");
    Expect.AreEqual(southWest, cardinals[2], "Cardinals (2,2) East");
    Expect.AreEqual(northWest, cardinals[3], "Cardinals (2,2) West");

    // 2x2 to NxN
    for (int x = 2; x <= MaxSize; x++)
    {
      for (int z = 2; z <= MaxSize; z++)
      {
        // 2x2 is a special case, handled earlier. But we still need to test 2x3 and 3x2
        if (x == 2 && z == 2) continue;
        cellRect = CellRect.CenteredOn(position, x, z);

        north = new IntVec3(position.x, 0, cellRect.maxZ);
        south = new IntVec3(position.x, 0, cellRect.minZ);
        east = new IntVec3(cellRect.maxX, 0, position.z);
        west = new IntVec3(cellRect.minX, 0, position.z);

        cardinals = cellRect.Cardinals().ToList();
        Expect.AreEqual(cardinals.Count, 4, $"Cardinals ({x},{z}) Count");
        Expect.AreEqual(north, cardinals[0], $"Cardinals ({x},{z}) North");
        Expect.AreEqual(south, cardinals[1], $"Cardinals ({x},{z}) South");
        Expect.AreEqual(east, cardinals[2], $"Cardinals ({x},{z}) East");
        Expect.AreEqual(west, cardinals[3], $"Cardinals ({x},{z}) West");
      }
    }
  }

  [Test]
  private void AllCellsNoRepeat()
  {
    // t shape overlap
    Test("Cross", new CellRect(0, 2, 8, 4), new CellRect(2, 0, 4, 8));
    // T shape overlap
    Test("T_Left", new CellRect(0, 2, 8, 4), new CellRect(0, 0, 4, 8));
    Test("T_Right", new CellRect(0, 2, 8, 4), new CellRect(4, 0, 4, 8));
    Test("T_Top", new CellRect(0, 4, 8, 4), new CellRect(2, 0, 4, 8));
    Test("T_Bottom", new CellRect(0, 0, 8, 4), new CellRect(2, 0, 4, 8));
    // L shape overlap
    Test("L_BottomLeft", new CellRect(0, 0, 8, 4), new CellRect(0, 0, 4, 8));
    Test("L_TopLeft", new CellRect(0, 4, 8, 4), new CellRect(0, 0, 4, 8));
    Test("L_TopRight", new CellRect(0, 4, 8, 4), new CellRect(4, 0, 4, 8));
    Test("L_BottomRight", new CellRect(0, 0, 8, 4), new CellRect(4, 0, 4, 8));
    // Partial overlap
    Test("LeftHanging", new CellRect(4, 2, 8, 4), new CellRect(2, 0, 4, 8));
    Test("RightHanging", new CellRect(0, 2, 8, 4), new CellRect(6, 0, 4, 8));
    Test("TopHanging", new CellRect(0, 6, 8, 4), new CellRect(2, 0, 4, 8));
    Test("BottomHanging", new CellRect(0, 0, 8, 4), new CellRect(2, 2, 4, 8));
    // Corner overlap
    Test("BottomLeftCorner", new CellRect(0, 0, 8, 4), new CellRect(6, 2, 4, 8));
    Test("TopLeftCorner", new CellRect(0, 6, 8, 4), new CellRect(6, 0, 4, 8));
    Test("TopRightCorner", new CellRect(2, 6, 8, 4), new CellRect(0, 0, 4, 8));
    Test("BottomRightCorner", new CellRect(2, 0, 8, 4), new CellRect(0, 2, 4, 8));
    // Full overlap
    Test("Full", new CellRect(0, 2, 8, 4), new CellRect(0, 2, 8, 4));
    // No overlap
    Test("None", new CellRect(0, 0, 8, 4), new CellRect(10, 10, 4, 8));

    return;

    static void Test(string label, CellRect cellRect, CellRect otherRect)
    {
      HashSet<IntVec3> uniqueCells = cellRect.Cells.Concat(otherRect.Cells).Distinct().ToHashSet();
      List<IntVec3> extensionCells = cellRect.AllCellsNoRepeat(otherRect).ToList();

      // Distinct cells in result
      Expect.AreEqual(extensionCells.Count, uniqueCells.Count,
        $"AllCellsNoRepeat_{label} (Distinct)");
      // Result is the same as if we enumerated both separately and filtered out duplicates
      Expect.All(extensionCells, uniqueCells.Contains, $"AllCellsNoRepeat_{label} (Correct)");

      // Ensure that order of rects does not matter
      Gen.Swap(ref cellRect, ref otherRect);

      uniqueCells = cellRect.Cells.Concat(otherRect.Cells).Distinct().ToHashSet();
      extensionCells = cellRect.AllCellsNoRepeat(otherRect).ToList();

      // Distinct cells in result
      Expect.AreEqual(extensionCells.Count, uniqueCells.Count,
        $"AllCellsNoRepeat_Swap_{label} (Distinct)");
      // Result is the same as if we enumerated both separately and filtered out duplicates
      Expect.All(extensionCells, uniqueCells.Contains, $"AllCellsNoRepeat_Swap_{label} (Correct)");
    }
  }

  [Test]
  private void EdgeCellsSpan()
  {
    const int MapSize = 250;
    const int Padding = 5;

    CellRect mapRect = new(0, 0, MapSize, MapSize);

    // North
    CellRect cellRect = mapRect.EdgeCellsSpan(Rot4.North, size: Padding);
    CellRect resultRect = new(0, MapSize - Padding, MapSize, Padding);
    Expect.AreEqual(cellRect, resultRect, "EdgeCellsSpan North");
    Expect.AreEqual(resultRect.Cells.Count(), MapSize * Padding, "EdgeCellsSpan North CellCount");

    // East
    cellRect = mapRect.EdgeCellsSpan(Rot4.East, size: Padding);
    resultRect = new CellRect(MapSize - Padding, 0, Padding, MapSize);
    Expect.AreEqual(cellRect, resultRect, "EdgeCellsSpan East");
    Expect.AreEqual(resultRect.Cells.Count(), MapSize * Padding, "EdgeCellsSpan East CellCount");

    // South
    cellRect = mapRect.EdgeCellsSpan(Rot4.South, size: Padding);
    resultRect = new CellRect(0, 0, MapSize, Padding);
    Expect.AreEqual(cellRect, resultRect, "EdgeCellsSpan South");
    Expect.AreEqual(resultRect.Cells.Count(), MapSize * Padding, "EdgeCellsSpan South CellCount");

    // West
    cellRect = mapRect.EdgeCellsSpan(Rot4.West, size: Padding);
    resultRect = new CellRect(0, 0, Padding, MapSize);
    Expect.AreEqual(cellRect, resultRect, "EdgeCellsSpan West");
    Expect.AreEqual(resultRect.Cells.Count(), MapSize * Padding, "EdgeCellsSpan West CellCount");
  }
}