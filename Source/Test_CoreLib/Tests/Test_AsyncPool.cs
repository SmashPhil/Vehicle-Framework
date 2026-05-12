using System;
using CoreLib.Performance;
using DevTools.Testing;
using UnityEngine.Assertions;
using AsyncPool =
  CoreLib.Performance.AsyncPool<CoreLib.Testing.Test_AsyncPool.TestObject>;

namespace CoreLib.Testing;

[TestFixture(TestType.MainMenu)]
[TestDescription("Thread safe global object pool.")]
internal class Test_AsyncPool
{
  private const int PreWarmCount = 5;

  [TearDown]
  private void ClearPool()
  {
    AsyncPool.Clear();
  }

  [Test, ExecutionPriority(Priority.First)]
  private void PreWarm()
  {
    Assert.AreEqual(AsyncPool.Count, 0);
    using ObjectCountWatcher<TestObject> ocw = new();
    AsyncPool.PreWarm(PreWarmCount);
    Expect.AreEqual(AsyncPool.Count, PreWarmCount);
    Expect.AreEqual(ocw.Count, PreWarmCount);
  }

  [Test, ExecutionPriority(Priority.First)]
  private void Clear()
  {
    AsyncPool.PreWarm(PreWarmCount);
    Assert.IsTrue(AsyncPool.Count > 0);
    using ObjectCountWatcher<TestObject> ocw = new();
    AsyncPool.Clear();
    Expect.AreEqual(AsyncPool.Count, 0);
    Expect.AreEqual(ocw.Count, 0);
  }

  [Test, ExecutionPriority(Priority.AboveNormal)]
  private void GetNew()
  {
    AsyncPool.Clear();
    Assert.AreEqual(0, AsyncPool.Count);
    using ObjectCountWatcher<TestObject> ocw = new();
    _ = AsyncPool.Get();
    Expect.AreEqual(ocw.Count, 1);
  }

  [Test, ExecutionPriority(Priority.AboveNormal)]
  private void GetCached()
  {
    AsyncPool.Clear();
    Assert.AreEqual(AsyncPool.Count, 0);
    AsyncPool.PreWarm(1);
    Assert.AreEqual(AsyncPool.Count, 1);

    using ObjectCountWatcher<TestObject> ocw = new();
    _ = AsyncPool.Get();
    Expect.AreEqual(AsyncPool.Count, 0);
    Expect.AreEqual(ocw.Count, 0);
  }

  [Test, ExecutionPriority(Priority.AboveNormal)]
  private void Return()
  {
    AsyncPool.Clear();
    Assert.AreEqual(AsyncPool.Count, 0);
    TestObject testObject = AsyncPool.Get();
    using ObjectCountWatcher<TestObject> ocw = new();
    AsyncPool.Return(testObject);
    Expect.AreEqual(AsyncPool.Count, 1);
    Expect.AreEqual(ocw.Count, 0);
  }

  [Test]
  private void GetScope()
  {
    AsyncPool.Clear();
    Assert.AreEqual(expected: 0, AsyncPool.Count);
    {
      using var scope = new AsyncPool.Scope(out _);
      Assert.AreEqual(expected: 0, AsyncPool.Count);
    }
    Assert.AreEqual(expected: 1, AsyncPool.Count);
  }

  internal class TestObject
  {
    public TestObject()
    {
      ObjectCounter.Increment<TestObject>();
    }
  }
}