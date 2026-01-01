using CoreLib.Performance;
using DevTools.Testing;
using UnityEngine.Assertions;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestDescription("IPoolable interface and its behavior with ObjectPool")]
internal class UnitTest_IPoolable
{
  private const int MockSize = 10;

  [Test]
  private void InPool()
  {
    ObjectPool<TestObject> pool = new(MockSize);
    Assert.IsTrue(pool.Count == 0);
    TestObject obj = new();
    Assert.IsFalse(obj.InPool);
    pool.Return(obj);
    Expect.IsTrue(obj.InPool);
    obj = pool.Get();
    Expect.IsFalse(obj.InPool);
  }

  [Test]
  private void Reset()
  {
    ObjectPool<TestObject> pool = new(MockSize);
    TestObject obj = new()
    {
      Set = true
    };
    pool.Return(obj);
    Assert.IsFalse(obj.Set);
  }

  private class TestObject : IPoolable
  {
    public bool InPool { get; set; }

    public bool Set { get; set; }

    void IPoolable.Reset()
    {
      Set = false;
    }
  }
}