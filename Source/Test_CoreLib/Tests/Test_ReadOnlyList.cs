using System;
using System.Collections.Generic;
using CoreLib.Collections;
using DevTools.Benchmarking;
using DevTools.Testing;
using Unity.Profiling;
using UnityEngine.Assertions;

namespace CoreLib.Testing;

[TestFixture(TestType.MainMenu)]
[TestDescription("Struct wrapper object for read-only access to list.")]
[TestCategory(TestCategoryNames.Performance, TestCategoryNames.Collections)]
internal class Test_ReadOnlyList
{
  [Test, ExecutionPriority(Priority.First)]
  public void Count()
  {
    List<int> list = [1, 2, 3];
    ReadOnlyList<int> view = new(list);
    Assert.AreEqual(list.Count, view.Count);
  }

  [Test]
  public void ReadIndexer()
  {
    List<int> list = [1, 2, 3];
    ReadOnlyList<int> view = new(list);
    Assert.AreEqual(list[0], view[0]);
    Assert.AreEqual(list[1], view[1]);
    Assert.AreEqual(list[2], view[2]);
  }

  [Test]
  public void WriteIndexer()
  {
    List<int> list = [1, 2, 3];
    ReadOnlyList<int> view = new(list);
    Expect.Throws<NotSupportedException>(() => view[0] = -1);
    Assert.AreEqual(list[0], view[0]);
  }

  [Test]
  public void IndexOf()
  {
    List<int> list = [5, 6, 7];
    ReadOnlyList<int> view = new(list);
    Assert.AreEqual(list.IndexOf(5), view.IndexOf(5));
    Assert.AreEqual(list.IndexOf(6), view.IndexOf(6));
    Assert.AreEqual(list.IndexOf(7), view.IndexOf(7));
  }

  [Test]
  public void Insert()
  {
    List<int> list = [1, 2, 3];
    ReadOnlyList<int> view = new(list);
    Expect.Throws<NotSupportedException>(() => view.Insert(1, 4));
    Assert.AreEqual(list.Count, view.Count);
    Assert.AreEqual(list[1], view[1]);
  }

  [Test]
  public void RemoveAt()
  {
    List<int> list = [1, 2, 3];
    ReadOnlyList<int> view = new(list);
    Expect.Throws<NotSupportedException>(() => view.RemoveAt(index: 1));
    Assert.AreEqual(list.Count, view.Count);
    Assert.AreEqual(list[1], view[1]);
  }

  [Test]
  public void Add()
  {
    List<int> list = [1, 2, 3];
    ReadOnlyList<int> view = new(list);
    Expect.Throws<NotSupportedException>(() => view.Add(4));
    Assert.AreEqual(list.Count, view.Count);
  }

  [Test]
  public void Remove()
  {
    List<int> list = [1, 2, 3];
    ReadOnlyList<int> view = new(list);
    Expect.Throws<NotSupportedException>(() => view.Remove(3));
    Assert.AreEqual(list.Count, view.Count);
    Assert.AreEqual(list[2], view[2]);
  }

  [Test]
  public void Clear()
  {
    List<int> list = [1, 2, 3];
    ReadOnlyList<int> view = new(list);
    Expect.Throws<NotSupportedException>(() => view.Clear());
    Assert.AreEqual(list.Count, view.Count);
  }

  [Test]
  public void Contains()
  {
    List<int> list = [1, 2, 3];
    ReadOnlyList<int> view = new(list);
    Assert.IsTrue(view.Contains(1));
    Assert.IsTrue(view.Contains(2));
    Assert.IsTrue(view.Contains(3));
    Assert.IsFalse(view.Contains(4));
  }

  [Test]
  public void CopyTo()
  {
    List<int> list = [1, 2, 3];
    ReadOnlyList<int> view = new(list);
    int[] copy = new int[list.Count];
    view.CopyTo(copy, 0);
    Assert.AreEqual(list[0], copy[0]);
    Assert.AreEqual(list[1], copy[1]);
    Assert.AreEqual(list[2], copy[2]);
  }

  [Test]
  public void NonAllocatingEnumerator()
  {
    List<int> list = [1, 2, 3];
    ReadOnlyList<int> view = new(list);
    using ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
    long bytes = recorder.CurrentValue;
    Assert.IsTrue(recorder.Valid);
    foreach (int item in view)
    {
      // JIT keeps eliminating this loop in release builds
      DeadCodeHelper.Consume(item);
    }
    Assert.AreEqual(expected: bytes, recorder.CurrentValue);
  }
}