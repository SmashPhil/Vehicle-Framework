using DevTools.Testing;
using SmashTools.Performance;
using UnityEngine.Assertions;
using AsyncPool =
  SmashTools.Performance.AsyncPool<SmashTools.UnitTesting.UnitTest_AsyncPool.TestObject>;

namespace SmashTools.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestDescription("Thread safe global object pool.")]
internal class UnitTest_AsyncPool
{
  [SetUp, TearDown]
  private void ClearPool()
  {
    AsyncPool.Clear();
  }

  [Test]
  private void ItemExchange()
  {
    const int PreWarmCount = 5;

    Assert.AreEqual(AsyncPool.Count, 0);

    // PreWarm
    {
      using ObjectCountWatcher<TestObject> ocw = new();

      AsyncPool.PreWarm(PreWarmCount);
      Expect.AreEqual(AsyncPool.Count, PreWarmCount, "PreWarm Init");
      Expect.AreEqual(ocw.Count, PreWarmCount, "New Objects");
    }

    TestObject testObject;
    // Get
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      testObject = AsyncPool.Get();
      Expect.AreEqual(AsyncPool.Count, PreWarmCount - 1, "Get Item--");
      Expect.AreEqual(ocw.Count, 0, "Get New Objects");
    }

    // Return
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      AsyncPool.Return(testObject);
      Expect.AreEqual(AsyncPool.Count, PreWarmCount, "Return Item++");
      Expect.AreEqual(ocw.Count, 0, "Return New Objects");
    }

    // Dump
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      AsyncPool.Clear();
      Expect.AreEqual(AsyncPool.Count, 0, "Dump Items");
      Expect.AreEqual(ocw.Count, 0, "Dump New Objects");
    }

    // Get (Create New)
    {
      using ObjectCountWatcher<TestObject> ocw = new();
      _ = AsyncPool.Get();
      Expect.AreEqual(ocw.Count, 1, "Get New Objects");
    }
  }

  internal class TestObject
  {
    public TestObject()
    {
      ObjectCounter.Increment<TestObject>();
    }
  }
}