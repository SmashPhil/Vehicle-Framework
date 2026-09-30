
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestIndexToCellJob
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
}