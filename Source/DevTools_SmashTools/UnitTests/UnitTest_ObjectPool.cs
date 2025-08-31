using DevTools.Testing;
using SmashTools.Performance;
using UnityEngine.Assertions;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestDescription("Local object pool for specific types of objects used within the same context.")]
internal class UnitTest_ObjectPool
{
  [Test]
  private void ItemExchange()
  {
    const int PreWarmCount = 5;

    ObjectPool<TestObject> pool = new(10);
    Assert.IsTrue(pool.Count == 0);

    // PreWarm
    {
      using ObjectCountWatcher<TestObject> ocw = new();

      pool.PreWarm(PreWarmCount);
      Expect.AreEqual(pool.Count, PreWarmCount, "PreWarm Init");
      Expect.AreEqual(ocw.Count, PreWarmCount, "New Objects");
    }

    // Create new object before we start watching object count, in practice
    // this object would've already been in use before being returned to pool.
    TestObject testObject = new();

    // Return
    {
      using ObjectCountWatcher<TestObject> ocw = new();

      pool.Return(testObject);
      Expect.IsTrue(testObject.InPool, "Return InPool");
      Expect.IsTrue(testObject.IsReset, "Return Reset");
      Expect.AreEqual(pool.Count, PreWarmCount + 1, "Return Head++");
      Expect.AreEqual(ocw.Count, 0, "Return New Objects");
    }

    // Get
    {
      using ObjectCountWatcher<TestObject> ocw = new();

      TestObject fetchedObject = pool.Get();
      Assert.IsFalse(testObject.IsReset);
      Expect.ReferencesAreEqual(testObject, fetchedObject, "Get Head");
      Expect.IsFalse(testObject.InPool, "Get InPool");
      Expect.AreEqual(pool.Count, PreWarmCount, "Get Head--");
      Expect.AreEqual(ocw.Count, 0, "Get New Objects");
    }

    // Dump
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      pool.Dump();
      Expect.AreEqual(pool.Count, 0, "Dump Head");
      Expect.AreEqual(ocw.Count, 0, "Dump New Objects");
    }

    // Get (Create New)
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      _ = pool.Get();
      Expect.AreEqual(pool.Count, 0, "Item not added to pool on new.");
      Expect.AreEqual(ocw.Count, 1, "Get New Objects");
    }
  }

  private class TestObject : IPoolable
  {
    private bool inPool;

    public TestObject()
    {
      ObjectCounter.Increment<TestObject>();
    }

    public bool IsReset { get; private set; }

    // Need backing field so we can simulate changing values when
    // object is fetched from pool by setting IsReset to false.
    public bool InPool
    {
      get { return inPool; }
      set
      {
        if (inPool == value) return;

        inPool = value;
        if (!inPool) IsReset = false;
      }
    }

    void IPoolable.Reset()
    {
      IsReset = true;
    }
  }
}