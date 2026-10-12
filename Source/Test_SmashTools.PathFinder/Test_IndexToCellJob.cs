
using DevTools.Testing;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_IndexToCellJob
{
  [Test]
  public void IndexToCellConversion()
  {
    int2 size = new(100, 100);
    int length = size.x * size.y;
    using NativeArray<uint> indexToCell = new(length, Allocator.TempJob);

    IndexToCellJob job = new(size, indexToCell);
    job.Run();

    for (int i = 0; i < length; i++)
    {
      int expectedX = i % size.x;
      int expectedY = i / size.x;
      uint pos = indexToCell[i];
      IndexLookupCache.Unpack(pos, out int x, out int y);
      Assert.AreEqual(expectedX, x);
      Assert.AreEqual(expectedY, y);
    }
  }
}
