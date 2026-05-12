using System.Collections;
using System.Collections.Generic;
using CoreLib.Collections;
using DevTools.Benchmarking;
using DevTools.Testing;
using Unity.Profiling;
using UnityEngine.Assertions;

namespace CoreLib.Testing;

[TestFixture(TestType.MainMenu)]
[TestDescription("Struct wrapper object for read-only access to array.")]
[TestCategory(TestCategoryNames.Performance, TestCategoryNames.Collections)]
internal class Test_ReadOnlyArray
{
  [Test, ExecutionPriority(Priority.First)]
  public void Length()
  {
    int[] array = [1, 2, 3];
    ReadOnlyArray<int> view = new(array);
    Expect.AreEqual(array.Length, view.Length);
  }

  [Test]
  public void ReadIndexer()
  {
    int[] array = [1, 2, 3];
    ReadOnlyArray<int> view = new(array);
    Expect.AreEqual(array[0], view[0]);
    Expect.AreEqual(array[1], view[1]);
    Expect.AreEqual(array[2], view[2]);
  }

  [Test]
  public void Enumerate()
  {
    int[] array = [1, 2, 3];
    ReadOnlyArray<int> view = new(array);

    int index = 0;
    foreach (int item in view)
    {
      Expect.AreEqual(array[index++], item);
    }

    Expect.AreEqual(array.Length, index);
  }

  [Test]
  public void GenericInterfaceEnumerate()
  {
    int[] array = [1, 2, 3];
    IEnumerable<int> view = new ReadOnlyArray<int>(array);

    int index = 0;
    foreach (int item in view)
    {
      Expect.AreEqual(array[index++], item);
    }
    Expect.AreEqual(array.Length, index);
  }

  [Test]
  public void NonGenericInterfaceEnumerate()
  {
    int[] array = [1, 2, 3];
    IEnumerable view = new ReadOnlyArray<int>(array);

    int index = 0;
    foreach (object item in view)
    {
      Expect.AreEqual(array[index++], item);
    }
    Expect.AreEqual(array.Length, index);
  }

  [Test]
  public void NonAllocatingEnumerator()
  {
    int[] array = [1, 2, 3];
    ReadOnlyArray<int> view = new(array);
    using ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Used Memory");
    long bytes = recorder.CurrentValue;
    Assert.IsTrue(recorder.Valid);

    foreach (int item in view)
    {
      // JIT keeps eliminating this loop in release builds
      DeadCodeHelper.Consume(item);
    }

    Expect.AreEqual(expected: bytes, recorder.CurrentValue);
  }
}