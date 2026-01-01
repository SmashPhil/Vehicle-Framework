using System;
using DevTools.Testing;
using CoreLib.Performance;
using UnityEngine.Assertions;

namespace CoreLib.UnitTesting;

[UnitTest(TestType.MainMenu)]
[TestDescription("Local object pool for specific types of objects used within the same context.")]
internal class UnitTest_ObjectPool
{
  private const int MockSize = 10;

  [Test, ExecutionPriority(Priority.First)]
  private void Constructor()
  {
    ObjectPool<TestObject> pool = new(MockSize);
    Assert.IsTrue(pool.Count == 0);
    Assert.IsFalse(pool.Resizable);
  }

  [Test, ExecutionPriority(Priority.First)]
  private void Return()
  {
    ObjectPool<TestObject> pool = new(MockSize);
    Assert.IsTrue(pool.Count == 0);
    TestObject obj = new();
    pool.Return(obj);
    Expect.AreEqual(1, pool.Count);
  }

  [Test]
  private void PreWarm()
  {
    const int PreWarmCount = 5;

    using ObjectCountWatcher<TestObject> ocw = new();
    ObjectPool<TestObject> pool = new(MockSize, PreWarmCount);
    Expect.AreEqual(PreWarmCount, pool.Count);
    Expect.AreEqual(PreWarmCount, ocw.Count);
  }

  [Test]
  private void GetEmpty()
  {
    using ObjectCountWatcher<TestObject> ocw = new();
    ObjectPool<TestObject> pool = new(MockSize);
    Assert.IsTrue(pool.Count == 0);
    _ = pool.Get();
    Expect.IsTrue(pool.Count == 0);
    Expect.AreEqual(1, ocw.Count);
  }

  [Test]
  private void GetScope()
  {
    ObjectPool<TestObject> pool = new(MockSize);
    {
      Assert.AreEqual(expected: 0, pool.Count);
      using var scope = pool.GetTemporary(out _);
      Assert.AreEqual(expected: 0, pool.Count);
    }
    Assert.AreEqual(expected: 1, pool.Count);
  }

  [Test]
  private void Clear()
  {
    ObjectPool<TestObject> pool = new(MockSize);
    pool.PreWarm(MockSize);
    Assert.AreEqual(expected: MockSize, pool.Count);
    pool.Clear();
    Assert.AreEqual(expected: 0, pool.Count);
  }

  [Test]
  private void Grow()
  {
    ObjectPool<TestObject> pool = new(MockSize, preWarm: MockSize)
    {
      Resizable = true
    };
    Assert.IsTrue(pool.GrowthFactor > 1);
    Assert.IsTrue(pool.Resizable);
    Assert.AreEqual(expected: MockSize, pool.Size);
    pool.Return(new TestObject());
    Assert.AreEqual(expected: MockSize * pool.GrowthFactor, pool.Size);
  }

  [Test]
  private void FixedSize()
  {
    ObjectPool<TestObject> pool = new(MockSize, preWarm: MockSize)
    {
      Resizable = false
    };
    Assert.IsFalse(pool.Resizable);
    Assert.AreEqual(expected: MockSize, pool.Size);
    pool.Return(new TestObject());
    Assert.AreEqual(expected: MockSize, pool.Size);
  }

  [Test]
  private void ClearDisposable()
  {
    ObjectPool<TestObjectDisposable> pool = new(MockSize);
    TestObjectDisposable obj = new();
    Assert.IsFalse(obj.Disposed);
    pool.Return(obj);
    Assert.AreEqual(expected: 1, pool.Count);
    pool.Clear();
    Expect.AreEqual(0, pool.Count);
    Expect.IsTrue(obj.Disposed);
  }

  [Test]
  private void ReturnDisposableFull()
  {
    ObjectPool<TestObjectDisposable> pool = new(MockSize);
    pool.PreWarm(MockSize);
    Assert.AreEqual(expected: MockSize, pool.Count);
    TestObjectDisposable obj = new();
    Assert.IsFalse(obj.Disposed);
    pool.Return(obj);
    Expect.AreEqual(MockSize, pool.Count);
    Expect.IsTrue(obj.Disposed);
  }

  private class TestObject
  {
    public TestObject()
    {
      ObjectCounter.Increment<TestObject>();
    }
  }

  private class TestObjectDisposable : IDisposable
  {
    public TestObjectDisposable()
    {
      ObjectCounter.Increment<TestObjectDisposable>();
    }

    public bool Disposed { get; private set; }

    public void Dispose()
    {
      Disposed = true;
    }
  }
}