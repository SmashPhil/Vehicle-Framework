using CoreLib.Rendering;
using DevTools.Testing;

namespace CoreLib.Tests;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Utils)]
[TestDescription("Grid ray casting against blocked cells.")]
internal class Test_RayCast
{
  private const int MapSize = 8;

  [Test]
  private void SourceCellIsClear()
  {
    RayCast<TestBlocker> ray = CreateRay(3, 3);

    Expect.IsTrue(ray.Hit(3, 3));
  }

  [Test]
  private void BlockedSourceDoesNotStopRay()
  {
    RayCast<TestBlocker> ray = CreateRay(3, 3, (3, 3));

    Expect.IsTrue(ray.Hit(5, 3));
  }

  [Test]
  private void ClearHorizontalRay()
  {
    RayCast<TestBlocker> ray = CreateRay(3, 3);

    Expect.IsTrue(ray.Hit(6, 3));
  }

  [Test]
  private void ClearVerticalRayInReverseDirection()
  {
    RayCast<TestBlocker> ray = CreateRay(3, 3);

    Expect.IsTrue(ray.Hit(3, 0));
  }

  [Test]
  private void ClearShallowDiagonalRay()
  {
    RayCast<TestBlocker> ray = CreateRay(5, 5);

    Expect.IsTrue(ray.Hit(1, 3));
  }

  [Test]
  private void ClearSteepDiagonalRay()
  {
    RayCast<TestBlocker> ray = CreateRay(2, 1);

    Expect.IsTrue(ray.Hit(4, 6));
  }

  [Test]
  private void BlockedDestinationStopsRay()
  {
    RayCast<TestBlocker> ray = CreateRay(3, 3, (6, 3));

    Expect.IsFalse(ray.Hit(6, 3));
  }

  [Test]
  private void BlockedIntermediateCellStopsRay()
  {
    RayCast<TestBlocker> ray = CreateRay(3, 3, (4, 3));

    Expect.IsFalse(ray.Hit(6, 3));
  }

  [Test]
  private void BlockerBesideRayDoesNotStopIt()
  {
    RayCast<TestBlocker> ray = CreateRay(3, 3, (4, 4));

    Expect.IsTrue(ray.Hit(6, 3));
  }

  [Test]
  private void OneBlockedSideAllowsDiagonalStep()
  {
    RayCast<TestBlocker> ray = CreateRay(3, 3, (4, 3));

    Expect.IsTrue(ray.Hit(4, 4));
  }

  [Test]
  private void TwoBlockedSidesStopDiagonalStep()
  {
    RayCast<TestBlocker> ray = CreateRay(3, 3, (4, 3), (3, 4));

    Expect.IsFalse(ray.Hit(4, 4));
  }

  private static RayCast<TestBlocker> CreateRay(int originX, int originY,
    params (int x, int y)[] blockedCells)
  {
    bool[] blocked = new bool[MapSize * MapSize];
    foreach ((int x, int y) in blockedCells)
    {
      blocked[y * MapSize + x] = true;
    }
    RayCast<TestBlocker>.Settings settings = new()
    {
      mapWidth = MapSize,
      blocker = new TestBlocker(blocked)
    };
    return new RayCast<TestBlocker>(originX, originY, in settings);
  }

  private readonly struct TestBlocker : ICellBlocker
  {
    private readonly bool[] blocked;

    public TestBlocker(bool[] blocked)
    {
      this.blocked = blocked;
    }

    public bool IsBlocked(int index)
    {
      return blocked[index];
    }
  }
}
