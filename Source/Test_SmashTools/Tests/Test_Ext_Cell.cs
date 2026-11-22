using DevTools.Testing;
using Verse;

namespace SmashTools.Tests;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Utils)]
[TestDescription("Cell offset rotation for rectangular footprints.")]
internal class Test_Ext_Cell
{
  [Test]
  private void RotatedBySingleCellOffset()
  {
    IntVec2 offset = new(0, 1);
    IntVec2 size = new(1, 1);

    Expect.AreEqual(new IntVec2(0, 1), offset.RotatedBy(Rot4.North, size), "North");
    Expect.AreEqual(new IntVec2(-1, 0), offset.RotatedBy(Rot4.East, size), "East");
    Expect.AreEqual(new IntVec2(0, -1), offset.RotatedBy(Rot4.South, size), "South");
    Expect.AreEqual(new IntVec2(1, 0), offset.RotatedBy(Rot4.West, size), "West");
  }

  [Test]
  private void MirrorRotatedBySingleCellOffset()
  {
    IntVec2 offset = new(0, 1);
    IntVec2 size = new(1, 1);

    Expect.AreEqual(new IntVec2(0, 1), offset.MirrorRotatedBy(Rot4.North, size), "North");
    Expect.AreEqual(new IntVec2(1, 0), offset.MirrorRotatedBy(Rot4.East, size), "East");
    Expect.AreEqual(new IntVec2(0, -1), offset.MirrorRotatedBy(Rot4.South, size), "South");
    Expect.AreEqual(new IntVec2(-1, 0), offset.MirrorRotatedBy(Rot4.West, size), "West");
  }

  [Test]
  private void RotatedByOddFootprint()
  {
    IntVec2 offset = new(1, 2);
    IntVec2 size = new(3, 5);

    Expect.AreEqual(new IntVec2(1, 2), offset.RotatedBy(Rot4.North, size), "North");
    Expect.AreEqual(new IntVec2(-2, 1), offset.RotatedBy(Rot4.East, size), "East");
    Expect.AreEqual(new IntVec2(-1, -2), offset.RotatedBy(Rot4.South, size), "South");
    Expect.AreEqual(new IntVec2(2, -1), offset.RotatedBy(Rot4.West, size), "West");
  }

  [Test]
  private void MirrorRotatedByOddFootprint()
  {
    IntVec2 offset = new(1, 2);
    IntVec2 size = new(3, 5);

    Expect.AreEqual(new IntVec2(1, 2), offset.MirrorRotatedBy(Rot4.North, size), "North");
    Expect.AreEqual(new IntVec2(2, -1), offset.MirrorRotatedBy(Rot4.East, size), "East");
    Expect.AreEqual(new IntVec2(-1, -2), offset.MirrorRotatedBy(Rot4.South, size), "South");
    Expect.AreEqual(new IntVec2(-2, 1), offset.MirrorRotatedBy(Rot4.West, size), "West");
  }

  [Test]
  private void RotatedByEvenFootprint()
  {
    IntVec2 offset = new(1, 2);
    IntVec2 size = new(2, 4);

    Expect.AreEqual(new IntVec2(1, 2), offset.RotatedBy(Rot4.North, size), "North");
    Expect.AreEqual(new IntVec2(-2, 1), offset.RotatedBy(Rot4.East, size), "East");
    Expect.AreEqual(new IntVec2(0, -1), offset.RotatedBy(Rot4.South, size), "South");
    Expect.AreEqual(new IntVec2(3, 0), offset.RotatedBy(Rot4.West, size), "West");
  }

  [Test]
  private void MirrorRotatedByEvenFootprint()
  {
    IntVec2 offset = new(1, 2);
    IntVec2 size = new(2, 4);

    Expect.AreEqual(new IntVec2(1, 2), offset.MirrorRotatedBy(Rot4.North, size), "North");
    Expect.AreEqual(new IntVec2(2, -1), offset.MirrorRotatedBy(Rot4.East, size), "East");
    Expect.AreEqual(new IntVec2(0, -1), offset.MirrorRotatedBy(Rot4.South, size), "South");
    Expect.AreEqual(new IntVec2(-1, 0), offset.MirrorRotatedBy(Rot4.West, size), "West");
  }

  [Test]
  private void RotatedByEvenFootprintRoot()
  {
    IntVec2 root = new(0, 0);
    IntVec2 size = new(2, 4);

    Expect.AreEqual(new IntVec2(0, 0), root.RotatedBy(Rot4.North, size), "North");
    Expect.AreEqual(new IntVec2(0, 0), root.RotatedBy(Rot4.East, size), "East");
    Expect.AreEqual(new IntVec2(1, 1), root.RotatedBy(Rot4.South, size), "South");
    Expect.AreEqual(new IntVec2(1, 1), root.RotatedBy(Rot4.West, size), "West");
  }

  [Test]
  private void MirrorRotatedByEvenFootprintRoot()
  {
    IntVec2 root = new(0, 0);
    IntVec2 size = new(2, 4);

    Expect.AreEqual(root, root.MirrorRotatedBy(Rot4.North, size), "North");
    Expect.AreEqual(root, root.MirrorRotatedBy(Rot4.East, size), "East");
    Expect.AreEqual(root, root.MirrorRotatedBy(Rot4.South, size), "South");
    Expect.AreEqual(root, root.MirrorRotatedBy(Rot4.West, size), "West");
  }
}
