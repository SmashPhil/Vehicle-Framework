using System.Collections.Generic;
using DevTools.Testing;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace CoreLib.Tests;

[TestFixture(TestType.MainMenu)]
[TestDescription("Diagonal Rect approximation based on width, height, and rotation.")]
internal class Test_EntityRect
{
  private static readonly Orientation Orientation = new(Orientation.NorthEast);

  private static List<int2> GetCellList(EntityRect rect)
  {
    List<int2> cells = [];
    using EntityRect.Enumerator enumerator = rect.GetEnumerator();
    while (enumerator.MoveNext())
    {
      cells.Add(enumerator.Current);
    }
    return cells;
  }

  [Test]
  public void OneByOne()
  {
    EntityRect rect = new(x: 0, y: 0, width: 1, height: 1, Orientation);
    List<int2> cells = GetCellList(rect);

    Assert.AreEqual(expected: 1, cells.Count);
    Assert.AreEqual(expected: new int2(0, 0), cells[0]);
  }

  [Test]
  public void OneByTwo()
  {
    EntityRect rect = new(x: 0, y: 0, width: 1, height: 2, Orientation);
    List<int2> cells = GetCellList(rect);

    Assert.AreEqual(expected: 1, cells.Count);
    Assert.AreEqual(expected: new int2(0, 0), cells[0]);
  }

  [Test]
  public void OneByThree()
  {
    HashSet<int2> cellSet = [new(0, 0), new(1, 1), new(-1, -1)];

    EntityRect rect = new(x: 0, y: 0, width: 1, height: 3, Orientation);
    List<int2> cells = GetCellList(rect);

    Assert.AreEqual(expected: 3, cells.Count);
    foreach (int2 cell in cells)
    {
      Assert.IsTrue(cellSet.Contains(cell));
    }
  }

  [Test]
  public void OneByFour()
  {
    HashSet<int2> cellSet = [new(0, 0), new(1, 1), new(-1, -1)];

    EntityRect rect = new(x: 0, y: 0, width: 1, height: 4, Orientation);
    List<int2> cells = GetCellList(rect);

    Assert.AreEqual(expected: 3, cells.Count);
    foreach (int2 cell in cells)
    {
      Assert.IsTrue(cellSet.Contains(cell));
    }
  }

  [Test]
  public void TwoByTwo()
  {
    HashSet<int2> cellSet = [
      new(0, 0), new(0, -1), new(1, -1),
      new(1, 0), new(2, 0), new(1, 1)];

    EntityRect rect = new(x: 0, y: 0, width: 2, height: 2, Orientation);
    List<int2> cells = GetCellList(rect);

    Assert.AreEqual(expected: 6, cells.Count);
    foreach (int2 cell in cells)
    {
      Assert.IsTrue(cellSet.Contains(cell));
    }
  }

  [Test]
  public void TwoByThree()
  {
    HashSet<int2> cellSet = [
      new(0, 0), new(-1, -1), new(0, -1),
      new(0, -2), new(1, -1), new(1, 0)];

    EntityRect rect = new(x: 0, y: 0, width: 2, height: 3, Orientation);
    List<int2> cells = GetCellList(rect);

    Assert.AreEqual(expected: 6, cells.Count);
    foreach (int2 cell in cells)
    {
      Assert.IsTrue(cellSet.Contains(cell));
    }
  }

  [Test]
  public void TwoByFour()
  {
    HashSet<int2> cellSet = [new(0, 0),
      new(-1, -1), new(0, -1), new(0, -2), new(1, -1),
      new(1, 0), new(1, 1), new(2, 0), new(2, 1)];

    EntityRect rect = new(x: 0, y: 0, width: 2, height: 4, Orientation);
    List<int2> cells = GetCellList(rect);

    Assert.AreEqual(expected: 9, cells.Count);
    foreach (int2 cell in cells)
    {
      Assert.IsTrue(cellSet.Contains(cell));
    }
  }

  [Test]
  public void ThreeByThree()
  {
    HashSet<int2> cellSet = [new(0, 0),
      new(-1, 0), new(1, 0), new(0, -1), new(0, 1),
      new(-1, -1), new(-1, 1), new(1, -1), new(1, 1),
      new(-2, 0), new(2, 0), new(0, -2), new(0, 2)];

    EntityRect rect = new(x: 0, y: 0, width: 3, height: 3, Orientation);
    List<int2> cells = GetCellList(rect);

    Assert.AreEqual(expected: 13, cells.Count);
    foreach (int2 cell in cells)
    {
      Assert.IsTrue(cellSet.Contains(cell));
    }
  }

  [Test]
  public void ThreeByFour()
  {
    HashSet<int2> cellSet = [new(0, 0),
      new(-1, 0), new(1, 0), new(0, -1), new(0, 1),
      new(-1, 1), new(1, -1), new(1, 1),
      new(0, 2), new(1, 2), new(2, 0), new(2, 1),
      new(-2, 0), new(-1, -1), new(0, -2)];

    EntityRect rect = new(x: 0, y: 0, width: 3, height: 4, Orientation);
    List<int2> cells = GetCellList(rect);

    Assert.AreEqual(expected: 15, cells.Count);
    foreach (int2 cell in cells)
    {
      Assert.IsTrue(cellSet.Contains(cell));
    }
  }

  [Test]
  public void ThreeByFive()
  {
    HashSet<int2> cellSet = [new(0, 0),
      new(-1, 0), new(1, 0), new(0, -1), new(0, 1),
      new(-1, -1), new(-1, 1), new(1, -1), new(1, 1),
      new(-2, 0), new(-2, -1), new(0, -2), new(-1, -2),
      new(2, 0), new(2, 1), new(1, 2), new(0, 2)];

    EntityRect rect = new(x: 0, y: 0, width: 3, height: 5, Orientation);
    List<int2> cells = GetCellList(rect);

    Assert.AreEqual(expected: 17, cells.Count);
    foreach (int2 cell in cells)
    {
      Assert.IsTrue(cellSet.Contains(cell));
    }
  }

  [Test]
  public void ThreeBySix()
  {
    HashSet<int2> cellSet = [new(0, 0),
      new(-1, 0), new(1, 0), new(0, -1), new(0, 1),
      new(-1, -1), new(-1, 1), new(1, -1), new(1, 1),
      new(-2, 0), new(-2, -1), new(0, -2), new(-1, -2),
      new(2, 0), new(2, 1), new(2, 2), new(1, 2), new(0, 2),
      new(1, 3), new(3, 1), new(-3, -1), new(-2, -2), new(-1, -3)];

    EntityRect rect = new(x: 0, y: 0, width: 3, height: 6, Orientation);
    List<int2> cells = GetCellList(rect);

    Assert.AreEqual(expected: 23, cells.Count);
    foreach (int2 cell in cells)
    {
      Assert.IsTrue(cellSet.Contains(cell));
    }
  }
}