using DevTools.Testing;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.PostGameExit)]
[TestCategory(TestCategoryNames.ComponentCache)]
[TestDescription("Map and detached map component cache clearing.")]
internal class UnitTest_WorldUnloaded
{
  [Test]
  private void ComponentCacheClearAll()
  {
    Expect.IsTrue(ComponentCache.PriorityComponentCount() == 0);
    Expect.IsTrue(ComponentCache.DetachedComponentCount() == 0);
  }
}