using System;
using System.Collections.Generic;
using CoreLib.PathFinding;
using DevTools.Testing;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_FindPathJobWithModifiers
{
  private const int X = PathFinder.ImpassableCost;
  private const int MapWidth = 5;
  private const int MapHeight = 5;

  private static readonly int2 Size = new(5, 5);

  private struct PathFinderData : IDisposable
  {
    public PathFinder pathFinder;
    public NativeArray<int> pathGrid;

    public void Dispose()
    {
      pathGrid.Dispose();
      pathFinder.Dispose();
    }
  }

  private static void SetGrid(PathFinderData pathData, int[,] grid)
  {
    pathData.pathFinder.NotifyWritingToGrid();
    GridHelper.SetGrid(pathData.pathGrid, grid);
  }

  private PathFinderData CreatePathFinderData(int width = MapWidth, int height = MapHeight, int2 hitbox = default)
  {
    NativeArray<int> pathGrid = new(width * height, Allocator.TempJob);
    PathFinder pathFinder = new(new PathFinder.Settings
    {
      mapSize = new int2(width, height),
      hitbox = hitbox,
      pathGrid = pathGrid.AsReadOnly(),
      poolObjects = false
    });
    return new PathFinderData { pathFinder = pathFinder, pathGrid = pathGrid };
  }

  // Because the offset doesn't make the cell impassable, corner cutting against the high-cost
  // offset is still a valid path.
  // -----------
  //  0 0 0 0 0
  //  0 0 + 0 0
  //  0 + B + 0
  //  + 0 B 0 +
  //  + 0 B 0 +
  [Test]
  public void PathWithConfig()
  {
    const short B = 50;
    using PathFinderData data = CreatePathFinderData();
    using MockConfig config = new(Size);
    GridHelper.SetGrid(config.Array, new short[,]
    {
        { 0, 0, 0, 0, 0, },
        { 0, 0, 0, 0, 0, },
        { 0, 0, B, 0, 0, },
        { 0, 0, B, 0, 0, },
        { 0, 0, B, 0, 0, }
    });
    SetGrid(data, new[,]
    {
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, }
      });
    PathRequest request = new()
    {
      start = new int3(0, 0, 0),
      end = new int3(4, 0, 0),
      costOffsets = new List<PathOffsetGrid> { config }
    };
    Path path = data.pathFinder.FindPath(request);
    GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
    GridHelper.ValidateOptimalPath(in request, path, expected: 2 * PathFinder.CostCardinal + 4 * PathFinder.CostDiagonal);
  }

  // Because the modifier doesn't make the cell impassable, corner cutting against the high-cost
  // modifier is still a valid path.
  // -----------
  //  0 0 0 0 0
  //  0 0 + 0 0
  //  0 + M + 0
  //  + 0 M 0 +
  //  + 0 M 0 +
  [Test]
  public void PathWithModifiers()
  {
    const string ModifierId = "IntTest";

    using ModifierGrid grid = new(Size.x * Size.y);
    using PathFinderData data = CreatePathFinderData();

    const ushort Modifier = 50;
    grid.AddModifier(ModifierId, index: 2, new Modifier
    {
      type = ModifierType.Multiply,
      value = Modifier
    });
    grid.AddModifier(ModifierId, index: 7, new Modifier
    {
      type = ModifierType.Multiply,
      value = Modifier
    });
    grid.AddModifier(ModifierId, index: 12, new Modifier
    {
      type = ModifierType.Multiply,
      value = Modifier
    });

    SetGrid(data, new[,]
    {
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, }
      });
    PathRequest request = new()
    {
      start = new int3(0, 0, 0),
      end = new int3(4, 0, 0),
      modifiers = grid
    };
    Path path = data.pathFinder.FindPath(request);
    GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
    GridHelper.ValidateOptimalPath(in request, path, expected: 2 * PathFinder.CostCardinal + 4 * PathFinder.CostDiagonal);
  }

  // Modifier Set overrides the high-cost offset, allowing for a shorter path.
  // -----------
  //  0 0 0 0 0
  //  0 0 0 0 0
  //  0 0 B 0 0
  //  0 0 + 0 0
  //  + + B + +
  [Test]
  public void PathWithModifiersAndOffsets()
  {
    const string ModifierId = "IntTest";

    using ModifierGrid grid = new(Size.x * Size.y);
    using PathFinderData data = CreatePathFinderData();

    const ushort Modifier = 1;
    grid.AddModifier(ModifierId, index: 7, new Modifier
    {
      type = ModifierType.Add,
      value = Modifier
    });

    const short B = 100;
    using MockConfig config = new(Size);
    GridHelper.SetGrid(config.Array, new short[,]
    {
        { 0, 0, 0, 0, 0, },
        { 0, 0, 0, 0, 0, },
        { 0, 0, B, 0, 0, },
        { 0, 0, B, 0, 0, },
        { 0, 0, B, 0, 0, }
    });
    SetGrid(data, new[,]
    {
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, },
        { 1, 1, 1, 1, 1, }
      });
    PathRequest request = new()
    {
      start = new int3(0, 0, 0),
      end = new int3(4, 0, 0),
      modifiers = grid,
      costOffsets = new List<PathOffsetGrid> { config }
    };
    Path path = data.pathFinder.FindPath(request);
    GridHelper.ValidatePathResult(data.pathFinder.Snapshot, Size, request, path);
    GridHelper.ValidateOptimalPath(in request, path, expected: 2 * PathFinder.CostCardinal + 4 * PathFinder.CostDiagonal);
  }

  [Test]
  public void ImpassableWithOffsets()
  {
    const short B = 100;

    using PathFinderData data = CreatePathFinderData();
    using MockConfig config = new(Size);
    GridHelper.SetGrid(config.Array, new short[,]
    {
        { 0, 0, 0, 0, 0, },
        { 0, 0, 0, 0, 0, },
        { 0, 0, B, 0, 0, },
        { 0, 0, B, 0, 0, },
        { 0, 0, B, 0, 0, }
    });
    SetGrid(data, new[,]
    {
        { 1, 1, X, 1, 1, },
        { 1, 1, X, 1, 1, },
        { 1, 1, X, 1, 1, },
        { 1, 1, X, 1, 1, },
        { 1, 1, X, 1, 1, }
      });
    PathRequest request = new()
    {
      start = new int3(0, 0, 0),
      end = new int3(4, 0, 0),
      costOffsets = new List<PathOffsetGrid> { config }
    };
    Path path = data.pathFinder.FindPath(request);
    Assert.IsFalse(path.IsValid);
  }

  [Test]
  public void ImpassableWithOffsetsMultiCell()
  {
    const int MapLength = 10;
    const short B = 100;

    using PathFinderData data = CreatePathFinderData(MapLength, MapLength, hitbox: new int2(2, 4));
    using MockConfig config = new(new int2(MapLength, MapLength));
    GridHelper.SetGrid(config.Array, new short[,]
    {
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 },
        { 0, 0, 0, 0, B, 0, 0, 0, 0, 0 }
    });
    SetGrid(data, new[,]
    {
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 }
      });
    PathRequest request = new()
    {
      start = new int3(1, 0, 1),
      end = new int3(7, 0, 1),
      costOffsets = new List<PathOffsetGrid> { config }
    };
    Path path = data.pathFinder.FindPath(request);
    Assert.IsFalse(path.IsValid);
  }

  [Test]
  public void ImpassableWithModifiers()
  {
    const string ModifierId = "IntTest";

    using ModifierGrid grid = new(Size.x * Size.y);
    using PathFinderData data = CreatePathFinderData();

    const ushort Modifier = 50;
    grid.AddModifier(ModifierId, index: 2, new Modifier
    {
      type = ModifierType.Multiply,
      value = Modifier
    });
    grid.AddModifier(ModifierId, index: 7, new Modifier
    {
      type = ModifierType.Multiply,
      value = Modifier
    });
    grid.AddModifier(ModifierId, index: 12, new Modifier
    {
      type = ModifierType.Multiply,
      value = Modifier
    });

    SetGrid(data, new[,]
    {
        { 1, 1, X, 1, 1, },
        { 1, 1, X, 1, 1, },
        { 1, 1, X, 1, 1, },
        { 1, 1, X, 1, 1, },
        { 1, 1, X, 1, 1, }
      });
    PathRequest request = new()
    {
      start = new int3(0, 0, 0),
      end = new int3(4, 0, 0),
      modifiers = grid
    };
    Path path = data.pathFinder.FindPath(request);
    Assert.IsFalse(path.IsValid);
  }

  [Test]
  public void ImpassableWithModifiersMultiCell()
  {
    const int MapLength = 10;
    const string ModifierId = "IntTest";

    using ModifierGrid grid = new(MapLength * MapLength);
    using PathFinderData data = CreatePathFinderData(MapLength, MapLength, hitbox: new int2(2, 4));

    const ushort Modifier = 50;
    grid.AddModifier(ModifierId, index: 2, new Modifier
    {
      type = ModifierType.Multiply,
      value = Modifier
    });
    grid.AddModifier(ModifierId, index: 7, new Modifier
    {
      type = ModifierType.Multiply,
      value = Modifier
    });
    grid.AddModifier(ModifierId, index: 12, new Modifier
    {
      type = ModifierType.Multiply,
      value = Modifier
    });

    SetGrid(data, new[,]
    {
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 },
        { 1, 1, 1, 1, X, 1, 1, 1, 1, 1 }
      });
    PathRequest request = new()
    {
      start = new int3(1, 0, 1),
      end = new int3(7, 0, 1),
      modifiers = grid
    };
    Path path = data.pathFinder.FindPath(request);
    Assert.IsFalse(path.IsValid);
  }

  private class MockConfig : PathOffsetGrid
  {
    public MockConfig(int2 mapSize) : base(mapSize)
    {
    }

    public NativeArray<short> Array => array;
  }
}
