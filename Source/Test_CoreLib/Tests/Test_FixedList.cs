using System;
using CoreLib.Collections;
using DevTools.Testing;
using Unity.Collections;
using UnityEngine.Assertions;

namespace CoreLib.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_FixedList
{
  private const int MockCapacity = 8;

  [Test]
  public void Constructor()
  {
    NativeFixedList<int> list = new(length: MockCapacity, Allocator.Temp);
    Assert.IsTrue(list.IsCreated);
    Assert.AreEqual(expected: MockCapacity, list.Length);
    Assert.AreEqual(expected: 0, list.Count);
    list.Dispose();
    Assert.IsFalse(list.IsCreated);
  }

  [Test]
  public void Add()
  {
    using NativeFixedList<int> list = new(length: MockCapacity, Allocator.Temp);
    Assert.AreEqual(expected: 0, list.Count);
    list.Add(0);
    Assert.AreEqual(expected: 1, list.Count);
  }

  [Test]
  public void Remove()
  {
    const int Number = 5;

    using NativeFixedList<int> list = new(length: MockCapacity, Allocator.Temp);
    list.Add(Number);
    Assert.AreEqual(expected: 1, list.Count);
    Assert.AreEqual(expected: Number, list.Remove());
    Assert.AreEqual(expected: 0, list.Count);
  }

  [Test]
  public void Clear()
  {
    using NativeFixedList<int> list = new(length: MockCapacity, Allocator.Temp);
    list.Add(0);
    Assert.AreEqual(expected: 1, list.Count);
    list.Clear();
    Assert.AreEqual(expected: 0, list.Count);
  }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
  [Test]
  public void AddFull()
  {
    using NativeFixedList<int> list = new(length: 1, Allocator.Temp);
    list.Add(0);
    Expect.Throws<IndexOutOfRangeException>(() => list.Add(0));
  }
#endif
}
