using System.Collections.Generic;
using System.Linq;
using DevTools.Testing;
using Verse;

namespace SmashTools.Testing;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Utils)]
[TestDescription("CellRect enumerator struct for 2 potentially overlapping CellRects.")]
internal class Test_CellRectOverlap
{
  private static void ValidateNoDuplicates(CellRect cellRect, CellRect otherRect)
  {
    HashSet<IntVec3> uniqueCells = cellRect.Cells.Concat(otherRect.Cells).Distinct().ToHashSet();
    List<IntVec3> extensionCells = new CellRectOverlap(cellRect, otherRect).ToList();

    // Distinct cells in result
    Expect.AreEqual(extensionCells.Count, uniqueCells.Count);
    // Result is the same as if we enumerated both separately and filtered out duplicates
    Expect.All(extensionCells, uniqueCells.Contains);

    // Ensure that order of rects does not matter
    Gen.Swap(ref cellRect, ref otherRect);

    uniqueCells = cellRect.Cells.Concat(otherRect.Cells).Distinct().ToHashSet();
    extensionCells = new CellRectOverlap(cellRect, otherRect).ToList();

    // Distinct cells in result
    Expect.AreEqual(extensionCells.Count, uniqueCells.Count);
    // Result is the same as if we enumerated both separately and filtered out duplicates
    Expect.All(extensionCells, uniqueCells.Contains);
  }

  [Test]
  public void TShape()
  {
    ValidateNoDuplicates(new CellRect(0, 2, 8, 4), new CellRect(2, 0, 4, 8));
  }

  [Test]
  public void TLeft()
  {
    ValidateNoDuplicates(new CellRect(0, 2, 8, 4), new CellRect(0, 0, 4, 8));
  }

  [Test]
  public void TRight()
  {
    ValidateNoDuplicates(new CellRect(0, 2, 8, 4), new CellRect(4, 0, 4, 8));
  }

  [Test]
  public void TTop()
  {
    ValidateNoDuplicates(new CellRect(0, 4, 8, 4), new CellRect(2, 0, 4, 8));
  }

  [Test]
  public void TBottom()
  {
    ValidateNoDuplicates(new CellRect(0, 0, 8, 4), new CellRect(2, 0, 4, 8));
  }

  [Test]
  public void LBottomLeft()
  {
    ValidateNoDuplicates(new CellRect(0, 0, 8, 4), new CellRect(0, 0, 4, 8));
  }

  [Test]
  public void LTopLeft()
  {
    ValidateNoDuplicates(new CellRect(0, 4, 8, 4), new CellRect(0, 0, 4, 8));
  }

  [Test]
  public void LTopRight()
  {
    ValidateNoDuplicates(new CellRect(0, 4, 8, 4), new CellRect(4, 0, 4, 8));
  }

  [Test]
  public void LBottomRight()
  {
    ValidateNoDuplicates(new CellRect(0, 0, 8, 4), new CellRect(4, 0, 4, 8));
  }

  [Test]
  public void LeftHanging()
  {
    ValidateNoDuplicates(new CellRect(4, 2, 8, 4), new CellRect(2, 0, 4, 8));
  }

  [Test]
  public void RightHanging()
  {
    ValidateNoDuplicates(new CellRect(0, 2, 8, 4), new CellRect(6, 0, 4, 8));
  }

  [Test]
  public void TopHanging()
  {
    ValidateNoDuplicates(new CellRect(0, 6, 8, 4), new CellRect(2, 0, 4, 8));
  }

  [Test]
  public void BottomHanging()
  {
    ValidateNoDuplicates(new CellRect(0, 0, 8, 4), new CellRect(2, 2, 4, 8));
  }

  [Test]
  public void BottomLeftCorner()
  {
    ValidateNoDuplicates(new CellRect(0, 0, 8, 4), new CellRect(6, 2, 4, 8));
  }

  [Test]
  public void TopLeftCorner()
  {
    ValidateNoDuplicates(new CellRect(0, 6, 8, 4), new CellRect(6, 0, 4, 8));
  }

  [Test]
  public void TopRightCorner()
  {
    ValidateNoDuplicates(new CellRect(2, 6, 8, 4), new CellRect(0, 0, 4, 8));
  }

  [Test]
  public void BottomRightCorner()
  {
    ValidateNoDuplicates(new CellRect(2, 0, 8, 4), new CellRect(0, 2, 4, 8));
  }

  [Test]
  public void Equal()
  {
    ValidateNoDuplicates(new CellRect(0, 2, 8, 4), new CellRect(0, 2, 8, 4));
  }

  [Test]
  public void NoOverlap()
  {
    ValidateNoDuplicates(new CellRect(0, 0, 8, 4), new CellRect(10, 10, 4, 8));
  }
}