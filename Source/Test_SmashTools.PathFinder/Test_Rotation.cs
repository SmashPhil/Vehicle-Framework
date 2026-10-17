using DevTools.Testing;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_Rotation
{
  private struct Rot8
  {
    public const int North = 0;
    public const int East = 1;
    public const int South = 2;
    public const int West = 3;
    public const int NorthEast = 4;
    public const int SouthEast = 5;
    public const int SouthWest = 6;
    public const int NorthWest = 7;
  }
  
  [TestCase(Rot8.North, ExpectedResult = Rotation.North)]
  [TestCase(Rot8.East, ExpectedResult = Rotation.East)]
  [TestCase(Rot8.South, ExpectedResult = Rotation.South)]
  [TestCase(Rot8.West, ExpectedResult = Rotation.West)]
  [TestCase(Rot8.NorthEast, ExpectedResult = Rotation.NorthEast)]
  [TestCase(Rot8.SouthEast, ExpectedResult = Rotation.SouthEast)]
  [TestCase(Rot8.SouthWest, ExpectedResult = Rotation.SouthWest)]
  [TestCase(Rot8.NorthWest, ExpectedResult = Rotation.NorthWest)]
  public int ToOctant(int rot)
  {
    return Rotation.ToOctant(rot);
  }

  [TestCase(0, 0, 0, 1, ExpectedResult = Rotation.North)]
  [TestCase(0, 0, 1, 1, ExpectedResult = Rotation.NorthEast)]
  [TestCase(0, 0, 1, 0, ExpectedResult = Rotation.East)]
  [TestCase(0, 0, 1, -1, ExpectedResult = Rotation.SouthEast)]
  [TestCase(0, 0, 0, -1, ExpectedResult = Rotation.South)]
  [TestCase(0, 0, -1, -1, ExpectedResult = Rotation.SouthWest)]
  [TestCase(0, 0, -1, 0, ExpectedResult = Rotation.West)]
  [TestCase(0, 0, -1, 1, ExpectedResult = Rotation.NorthWest)]
  public int Heading(int x, int y, int nx, int ny)
  {
    return Rotation.Heading(x, y, nx, ny);
  }

  [Test]
  public void TurnCostInt4()
  {
    const int InitialHeading = Rotation.North;

    Assert.AreEqual(expected: new int4(0, 0, 0, 0), actual: Rotation.TurnCost(InitialHeading, new int4(0, 0, 0, 0)));
    Assert.AreEqual(expected: new int4(0, 1, 2, 3), actual: Rotation.TurnCost(InitialHeading, new int4(0, 1, 2, 3)));
    Assert.AreEqual(expected: new int4(4, 3, 2, 1), actual: Rotation.TurnCost(InitialHeading, new int4(4, 5, 6, 7)));
  }

  [TestCase(Rotation.North, ExpectedResult = 0)]
  [TestCase(Rotation.East, ExpectedResult = 2)]
  [TestCase(Rotation.South, ExpectedResult = 4)]
  [TestCase(Rotation.West, ExpectedResult = 2)]
  [TestCase(Rotation.NorthEast, ExpectedResult = 1)]
  [TestCase(Rotation.SouthEast, ExpectedResult = 3)]
  [TestCase(Rotation.SouthWest, ExpectedResult = 3)]
  [TestCase(Rotation.NorthWest, ExpectedResult = 1)]
  public int TurnCostInt(int heading)
  {
    const int InitialHeading = Rotation.North;
    return Rotation.TurnCost(InitialHeading, heading);
  }
}
