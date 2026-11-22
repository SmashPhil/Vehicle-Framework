using System;
using System.Collections;
using System.Collections.Generic;
using CoreLib.Performance;
using DevTools.Benchmarking;
using DevTools.Testing;

namespace CoreLib.Tests;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Performance, TestCategoryNames.Collections)]
[TestDescription("Pooled copy of a list for enumeration.")]
internal class Test_ListSnapshot
{
  [Test]
  private void Count()
  {
    using ListSnapshot<int> snapshot = new([1, 2, 3]);

    Expect.AreEqual(3, snapshot.Count);
  }

  [Test]
  private void EmptyList()
  {
    using ListSnapshot<int> snapshot = new([]);

    Expect.AreEqual(0, snapshot.Count);
  }

  [Test]
  private void SourceChangesDoNotAffectSnapshot()
  {
    List<int> source = [1, 2, 3];
    using ListSnapshot<int> snapshot = new(source);

    source[0] = 10;
    source.Add(4);

    Expect.AreEqual(1, snapshot.items[0]);
    Expect.AreEqual(3, snapshot.Count);
  }

  [Test]
  private void Enumerate()
  {
    using ListSnapshot<int> snapshot = new([1, 2, 3]);
    int[] expected = [1, 2, 3];

    int index = 0;
    foreach (int item in snapshot)
    {
      Expect.AreEqual(expected[index++], item);
    }
    Expect.AreEqual(expected.Length, index);
  }

  [Test]
  private void GenericInterfaceEnumerate()
  {
    using ListSnapshot<int> snapshot = new([1, 2, 3]);
    IEnumerable<int> enumerable = snapshot;
    int[] expected = [1, 2, 3];

    int index = 0;
    foreach (int item in enumerable)
    {
      Expect.AreEqual(expected[index++], item);
    }
    Expect.AreEqual(expected.Length, index);
  }

  [Test]
  private void NonGenericInterfaceEnumerate()
  {
    using ListSnapshot<int> snapshot = new([1, 2, 3]);
    IEnumerable enumerable = snapshot;
    int[] expected = [1, 2, 3];

    int index = 0;
    foreach (object item in enumerable)
    {
      Expect.AreEqual(expected[index++], item);
    }
    Expect.AreEqual(expected.Length, index);
  }

  [Test]
  private void DisposeClearsItems()
  {
    ListSnapshot<int> snapshot = new([1, 2, 3]);
    List<int> rented = snapshot.items;

    ((IDisposable)snapshot).Dispose();

    Expect.AreEqual(0, rented.Count);
  }

  [Test]
  private void DisposeReturnsItemsToPool()
  {
    ListSnapshot<object> snapshot = new([]);
    int countWhileRented = AsyncPool<List<object>>.Count;

    ((IDisposable)snapshot).Dispose();

    Expect.AreEqual(countWhileRented + 1, AsyncPool<List<object>>.Count);
  }

  [Test]
  private void NonAllocatingEnumerator()
  {
    using ListSnapshot<int> snapshot = new([1, 2, 3]);

    // Warmup JIT since it allocates on first pass
    foreach (int item in snapshot)
    {
      DeadCodeHelper.Consume(item);
    }

    long bytes = GC.GetAllocatedBytesForCurrentThread();
    foreach (int item in snapshot)
    {
      // JIT keeps eliminating this loop in release builds
      DeadCodeHelper.Consume(item);
    }
    Expect.AreEqual(expected: bytes, GC.GetAllocatedBytesForCurrentThread());
  }
}
