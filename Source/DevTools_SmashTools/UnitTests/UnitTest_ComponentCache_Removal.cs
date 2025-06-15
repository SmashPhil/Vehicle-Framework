using DevTools.UnitTesting;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.PostGameExit)]
[TestCategory(TestCategoryNames.ComponentCache)]
[TestDescription("Map and detached map component cache clearing.")]
internal class UnitTest_ComponentCache_Removal
{
  [Test]
  private void CacheCleared()
  {
    Expect.IsTrue(ComponentCache.PriorityComponentCount() == 0);
    Expect.IsTrue(ComponentCache.DetachedComponentCount() == 0);
  }
}