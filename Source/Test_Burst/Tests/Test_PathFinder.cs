using CoreLib.Performance;
using DevTools.Testing;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace SmashTools.Burst.Tests;

[TestClass(TestType.Playing), Disabled]
internal class Test_PathFinder
{
  private const int MapWidth = 5;
  private const int MapHeight = 5;

  [Test] // TODO
  public void PathFind()
  {
    Assert.IsTrue(UnityThread.IsInMainThread);

    NativeArray<int> pathGrid = new NativeArray<int>(MapWidth * MapHeight, Allocator.Persistent);
    using PathFinder pathFinder = new PathFinder(new PathFinder.Settings
    {
      mapSize = new int2(MapWidth, MapHeight),
      pathGrid = pathGrid.AsReadOnly(),
      poolObjects = false
    });
    for (int i = 0; i < pathGrid.Length; i++)
    {
      pathGrid[i] = 1;
    }
    PathRequest request = new() { start = new int3(0, 0, 0), end = new int3(1, 0, 0) };
    Path path = pathFinder.FindPath(request);
    Assert.IsTrue(path.Found);
    Assert.AreEqual(request.start, path.FirstNode);
    Assert.AreEqual(request.end, path.LastNode);
    Assert.AreEqual(expected: 2, path.NodesLeft);
    pathGrid.Dispose();
  }
}
