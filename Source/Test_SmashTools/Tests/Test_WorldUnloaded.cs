using DevTools.Testing;

namespace SmashTools.Testing;

[TestFixture(TestType.PostGameExit)]
[TestCategory(TestCategoryNames.ComponentCache)]
[TestDescription("Map and detached map component cache clearing.")]
internal class Test_WorldUnloaded
{
  [Test]
  private void ComponentCacheClearAll()
  {
    Expect.IsTrue(ComponentCache.PriorityComponentCount() == 0);
    Expect.IsTrue(ComponentCache.DetachedComponentCount() == 0);
  }
}