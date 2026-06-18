using System;
using System.Collections.Generic;
using System.Linq;
using DevTools.Testing;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace CoreLib.Tests;

[TestFixture(TestType.MainMenu)]
[TestDescription("Diagonal Rect approximation based on width, height, and rotation.")]
internal class Test_EntityRect
{
  [UsedWithReflection]
  private static readonly Orientation[] Orientations =
    [
      new(Orientation.North),
      new(Orientation.East),
      new(Orientation.South),
      new(Orientation.West),
      new(Orientation.NorthEast),
      new(Orientation.SouthEast),
      new(Orientation.SouthWest),
      new(Orientation.NorthWest),
    ];

  private static readonly HitboxResult OneByOneResult = new()
  {
    horizontal = [new int2(0, 0)],
    vertical = [new int2(0, 0)],
    northEast = [new int2(0, 0)],
    southEast = [new int2(0, 0)]
  };

  private static readonly HitboxResult OneByTwoResult = new()
  {
    horizontal = [new int2(0, 0), new int2(1, 0)],
    vertical = [new int2(0, 0), new int2(0, 1)],
    northEast = [new int2(0, 0)],
    southEast = [new int2(0, 0)]
  };

  private static readonly HitboxResult OneByThreeResult = new()
  {
    horizontal = [new int2(-1, 0), new int2(0, 0), new int2(1, 0)],
    vertical = [new int2(0, -1), new int2(0, 0), new int2(0, 1)],
    northEast = [new int2(0, 0), new int2(1, 1), new int2(-1, -1)],
    southEast = [new int2(0, 0), new int2(1, -1), new int2(-1, 1)]
  };

  private static readonly HitboxResult OneByFourResult = new()
  {
    horizontal = [new int2(-1, 0), new int2(0, 0), new int2(1, 0), new int2(2, 0)],
    vertical = [new int2(0, -1), new int2(0, 0), new int2(0, 1), new int2(0, 2)],
    northEast = [new int2(0, 0), new int2(1, 1), new int2(-1, -1)],
    southEast = [new int2(0, 0), new int2(1, -1), new int2(-1, 1)]
  };

  private static readonly HitboxResult TwoByTwoResult = new()
  {
    horizontal = [new int2(0, -1), new int2(1, -1), new int2(0, 0), new int2(1, 0)],
    vertical = [new int2(0, 0), new int2(1, 0), new int2(0, 1), new int2(1, 1)],
    northEast = [
      new int2(0, 0), new int2(0, -1), new int2(1, -1),
      new int2(1, 0), new int2(2, 0), new int2(1, 1)
    ],
    southEast = [
      new int2(-1, -1), new int2(-1, 0), new int2(0, 0),
      new int2(0, -1), new int2(1, -1), new int2(0, -2)
    ]
  };

  private static readonly HitboxResult TwoByThreeResult = new()
  {
    horizontal = [
      new int2(-1, -1), new int2(0, -1), new int2(1, -1),
      new int2(-1, 0), new int2(0, 0), new int2(1, 0)
    ],
    vertical = [
      new int2(0, -1), new int2(1, -1), new int2(0, 0),
      new int2(1, 0), new int2(0, 1), new int2(1, 1)
    ],
    northEast = [
      new int2(0, 0), new int2(-1, -1), new int2(0, -1),
      new int2(0, -2), new int2(1, -1), new int2(1, 0)
    ],
    southEast = [
      new int2(-1, -1), new int2(-2, 0), new int2(-1, 0),
      new int2(-1, 1), new int2(0, 0), new int2(0, -1)
    ]
  };

  private static readonly HitboxResult TwoByFourResult = new()
  {
    horizontal = [
      new int2(-1, -1), new int2(0, -1), new int2(1, -1), new int2(2, -1),
      new int2(-1, 0), new int2(0, 0), new int2(1, 0), new int2(2, 0)
    ],
    vertical = [
      new int2(0, -1), new int2(1, -1), new int2(0, 0), new int2(1, 0),
      new int2(0, 1), new int2(1, 1), new int2(0, 2), new int2(1, 2)
    ],
    northEast = [
      new int2(0, 0), new int2(-1, -1), new int2(0, -1),
      new int2(0, -2), new int2(1, -1), new int2(1, 0),
      new int2(1, 1), new int2(2, 0), new int2(2, 1)
    ],
    southEast = [
      new int2(-1, -1), new int2(-2, 0), new int2(-1, 0),
      new int2(-1, 1), new int2(0, 0), new int2(0, -1),
      new int2(0, -2), new int2(1, -1), new int2(1, -2)
    ]
  };

  private static readonly HitboxResult ThreeByThreeResult = new()
  {
    horizontal = [
      new int2(-1, -1), new int2(0, -1), new int2(1, -1),
      new int2(-1, 0), new int2(0, 0), new int2(1, 0),
      new int2(-1, 1), new int2(0, 1), new int2(1, 1)
    ],
    vertical = [
      new int2(-1, -1), new int2(0, -1), new int2(1, -1),
      new int2(-1, 0), new int2(0, 0), new int2(1, 0),
      new int2(-1, 1), new int2(0, 1), new int2(1, 1)
    ],
    northEast = [
      new int2(0, 0), new int2(-1, 0), new int2(1, 0), new int2(0, -1),
      new int2(0, 1), new int2(-1, -1), new int2(-1, 1), new int2(1, -1),
      new int2(1, 1), new int2(-2, 0), new int2(2, 0), new int2(0, -2),
      new int2(0, 2)
    ],
    southEast = [
      new int2(0, 0), new int2(-1, 0), new int2(1, 0), new int2(0, 1),
      new int2(0, -1), new int2(-1, 1), new int2(-1, -1), new int2(1, 1),
      new int2(1, -1), new int2(-2, 0), new int2(2, 0), new int2(0, 2),
      new int2(0, -2)
    ]
  };

  private static readonly HitboxResult ThreeByFourResult = new()
  {
    horizontal = [
      new int2(-1, -1), new int2(0, -1), new int2(1, -1), new int2(2, -1),
      new int2(-1, 0), new int2(0, 0), new int2(1, 0), new int2(2, 0),
      new int2(-1, 1), new int2(0, 1), new int2(1, 1), new int2(2, 1)
    ],
    vertical = [
      new int2(-1, -1), new int2(0, -1), new int2(1, -1),
      new int2(-1, 0), new int2(0, 0), new int2(1, 0),
      new int2(-1, 1), new int2(0, 1), new int2(1, 1),
      new int2(-1, 2), new int2(0, 2), new int2(1, 2)
    ],
    northEast = [
      new int2(0, 0), new int2(-1, 0), new int2(1, 0), new int2(0, -1),
      new int2(0, 1), new int2(-1, 1), new int2(1, -1), new int2(1, 1),
      new int2(0, 2), new int2(1, 2), new int2(2, 0), new int2(2, 1),
      new int2(-2, 0), new int2(-1, -1), new int2(0, -2)
    ],
    southEast = [
      new int2(0, 0), new int2(-1, 0), new int2(1, 0), new int2(0, 1),
      new int2(0, -1), new int2(-1, -1), new int2(1, 1), new int2(1, -1),
      new int2(0, -2), new int2(1, -2), new int2(2, 0), new int2(2, -1),
      new int2(-2, 0), new int2(-1, 1), new int2(0, 2)
    ]
  };

  private static readonly HitboxResult ThreeByFiveResult = new()
  {
    horizontal = [
      new int2(-2, -1), new int2(-1, -1), new int2(0, -1), new int2(1, -1),
      new int2(2, -1), new int2(-2, 0), new int2(-1, 0), new int2(0, 0),
      new int2(1, 0), new int2(2, 0), new int2(-2, 1), new int2(-1, 1),
      new int2(0, 1), new int2(1, 1), new int2(2, 1)
    ],
    vertical = [
      new int2(-1, -2), new int2(0, -2), new int2(1, -2),
      new int2(-1, -1), new int2(0, -1), new int2(1, -1),
      new int2(-1, 0), new int2(0, 0), new int2(1, 0),
      new int2(-1, 1), new int2(0, 1), new int2(1, 1),
      new int2(-1, 2), new int2(0, 2), new int2(1, 2)
    ],
    northEast = [
      new int2(0, 0), new int2(-1, 0), new int2(1, 0), new int2(0, -1),
      new int2(0, 1), new int2(-1, -1), new int2(-1, 1), new int2(1, -1),
      new int2(1, 1), new int2(-2, 0), new int2(-2, -1), new int2(0, -2),
      new int2(-1, -2), new int2(2, 0), new int2(2, 1), new int2(1, 2),
      new int2(0, 2)
    ],
    southEast = [
      new int2(0, 0), new int2(-1, 0), new int2(1, 0), new int2(0, 1),
      new int2(0, -1), new int2(-1, 1), new int2(-1, -1), new int2(1, 1),
      new int2(1, -1), new int2(-2, 0), new int2(-2, 1), new int2(0, 2),
      new int2(-1, 2), new int2(2, 0), new int2(2, -1), new int2(1, -2),
      new int2(0, -2)
    ]
  };

  private static readonly HitboxResult ThreeBySixResult = new()
  {
    horizontal = [
      new int2(-2, -1), new int2(-1, -1), new int2(0, -1), new int2(1, -1),
      new int2(2, -1), new int2(3, -1), new int2(-2, 0), new int2(-1, 0),
      new int2(0, 0), new int2(1, 0), new int2(2, 0), new int2(3, 0),
      new int2(-2, 1), new int2(-1, 1), new int2(0, 1), new int2(1, 1),
      new int2(2, 1), new int2(3, 1)
    ],
    vertical = [
      new int2(-1, -2), new int2(0, -2), new int2(1, -2),
      new int2(-1, -1), new int2(0, -1), new int2(1, -1),
      new int2(-1, 0), new int2(0, 0), new int2(1, 0),
      new int2(-1, 1), new int2(0, 1), new int2(1, 1),
      new int2(-1, 2), new int2(0, 2), new int2(1, 2),
      new int2(-1, 3), new int2(0, 3), new int2(1, 3)
    ],
    northEast = [
      new int2(0, 0), new int2(-1, 0), new int2(1, 0), new int2(0, -1),
      new int2(0, 1), new int2(-1, -1), new int2(-1, 1), new int2(1, -1),
      new int2(1, 1), new int2(-2, 0), new int2(-2, -1), new int2(0, -2),
      new int2(-1, -2), new int2(2, 0), new int2(2, 1), new int2(2, 2),
      new int2(1, 2), new int2(0, 2), new int2(1, 3), new int2(3, 1),
      new int2(-3, -1), new int2(-2, -2), new int2(-1, -3)
    ],
    southEast = [
      new int2(0, 0), new int2(-1, 0), new int2(1, 0), new int2(0, 1),
      new int2(0, -1), new int2(-1, 1), new int2(-1, -1), new int2(1, 1),
      new int2(1, -1), new int2(-2, 0), new int2(-2, 1), new int2(0, 2),
      new int2(-1, 2), new int2(2, 0), new int2(2, -1), new int2(2, -2),
      new int2(1, -2), new int2(0, -2), new int2(1, -3), new int2(3, -1),
      new int2(-3, 1), new int2(-2, 2), new int2(-1, 3)
    ]
  };

  private static List<int2> GetCellList(EntityRect rect)
  {
    List<int2> cells = [];
    using EntityRect.Enumerator enumerator = rect.GetEnumerator();
    while (enumerator.MoveNext())
    {
      cells.Add(enumerator.Current);
    }
    return cells.Distinct().ToList();
  }

  private static void AssertHitbox(EntityRect rect, HitboxResult result, Orientation orientation)
  {
    List<int2> cells = GetCellList(rect);
    HashSet<int2> hitbox = result[orientation];

    Assert.AreEqual(expected: hitbox.Count, cells.Count);
    foreach (int2 cell in cells)
    {
      Assert.IsTrue(hitbox.Contains(cell));
    }
  }

  [Test]
  public void OneByOne([ParametersSource("Orientations")] Orientation orientation)
  {
    EntityRect rect = new(x: 0, y: 0, width: 1, height: 1, orientation);
    AssertHitbox(rect, OneByOneResult, orientation);
  }

  [Test]
  public void OneByTwo([ParametersSource("Orientations")] Orientation orientation)
  {
    EntityRect rect = new(x: 0, y: 0, width: 1, height: 2, orientation);
    AssertHitbox(rect, OneByTwoResult, orientation);
  }

  [Test]
  public void OneByThree([ParametersSource("Orientations")] Orientation orientation)
  {
    EntityRect rect = new(x: 0, y: 0, width: 1, height: 3, orientation);
    AssertHitbox(rect, OneByThreeResult, orientation);
  }

  [Test]
  public void OneByFour([ParametersSource("Orientations")] Orientation orientation)
  {
    EntityRect rect = new(x: 0, y: 0, width: 1, height: 4, orientation);
    AssertHitbox(rect, OneByFourResult, orientation);
  }

  [Test]
  public void TwoByTwo([ParametersSource("Orientations")] Orientation orientation)
  {
    EntityRect rect = new(x: 0, y: 0, width: 2, height: 2, orientation);
    AssertHitbox(rect, TwoByTwoResult, orientation);
  }

  [Test]
  public void TwoByThree([ParametersSource("Orientations")] Orientation orientation)
  {
    EntityRect rect = new(x: 0, y: 0, width: 2, height: 3, orientation);
    AssertHitbox(rect, TwoByThreeResult, orientation);
  }

  [Test]
  public void TwoByFour([ParametersSource("Orientations")] Orientation orientation)
  {
    EntityRect rect = new(x: 0, y: 0, width: 2, height: 4, orientation);
    AssertHitbox(rect, TwoByFourResult, orientation);
  }

  [Test]
  public void ThreeByThree([ParametersSource("Orientations")] Orientation orientation)
  {
    EntityRect rect = new(x: 0, y: 0, width: 3, height: 3, orientation);
    AssertHitbox(rect, ThreeByThreeResult, orientation);
  }

  [Test]
  public void ThreeByFour([ParametersSource("Orientations")] Orientation orientation)
  {
    EntityRect rect = new(x: 0, y: 0, width: 3, height: 4, orientation);
    AssertHitbox(rect, ThreeByFourResult, orientation);
  }

  [Test]
  public void ThreeByFive([ParametersSource("Orientations")] Orientation orientation)
  {
    EntityRect rect = new(x: 0, y: 0, width: 3, height: 5, orientation);
    AssertHitbox(rect, ThreeByFiveResult, orientation);
  }

  [Test]
  public void ThreeBySix([ParametersSource("Orientations")] Orientation orientation)
  {
    EntityRect rect = new(x: 0, y: 0, width: 3, height: 6, orientation);
    AssertHitbox(rect, ThreeBySixResult, orientation);
  }

  [UsedWithReflection]
  private class HitboxResult
  {
    public HashSet<int2> northEast;
    public HashSet<int2> southEast;

    public HashSet<int2> vertical;
    public HashSet<int2> horizontal;

    public HashSet<int2> this[Orientation orientation]
    {
      get
      {
        return orientation.AsInt switch
        {
          0 => vertical,
          1 => horizontal,
          2 => vertical,
          3 => horizontal,
          4 => northEast,
          5 => southEast,
          6 => northEast,
          7 => southEast,
          _ => throw new InvalidOperationException()
        };
      }
    }
  }
}
