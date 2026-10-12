using System.Linq;
using DevTools.Testing;
using Unity.Collections;
using UnityEngine.Assertions;

namespace SmashTools.PathFinder.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_NativeArrayUtilities
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
