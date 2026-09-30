using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder.Tests
{
  [TestFixture]
  internal class TestNativeArrayUtilities
  {
    const int MockSize = 10;

    [Test]
    public void SetAll()
    {
      using NativeArray<int> array = new(MockSize, Allocator.Temp, options: NativeArrayOptions.ClearMemory);
      Assert.IsTrue(array.All(i => i == 0));
      NativeArrayUtility.SetAll(array, 0xFF);
      Assert.IsTrue(array.All(i => i == -1));
    }
  }
}