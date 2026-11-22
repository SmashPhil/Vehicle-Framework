using System;
using System.Threading;
using System.Threading.Tasks;
using CoreLib.Collections.Concurrent;
using DevTools.Testing;

namespace CoreLib.Tests;

[TestFixture(TestType.MainMenu)]
[TestCategory(TestCategoryNames.Collections, TestCategoryNames.Multithreading)]
[TestDescription("Fixed-capacity buffer for concurrent additions.")]
internal class Test_ConcurrentBuffer
{
  [Test]
  private void Constructor()
  {
    ConcurrentBuffer<int> buffer = new(3);

    Expect.AreEqual(0, buffer.Count);
    Expect.AreEqual(3, buffer.Capacity);
  }

  [Test]
  private void TryAddAtCapacity()
  {
    ConcurrentBuffer<int> buffer = new(2);

    Expect.IsTrue(buffer.TryAdd(10));
    Expect.IsTrue(buffer.TryAdd(20));
    Expect.AreEqual(2, buffer.Count);
    Expect.IsFalse(buffer.TryAdd(30));
    Expect.AreEqual(2, buffer.Count);
  }

  [Test]
  private void ZeroCapacity()
  {
    ConcurrentBuffer<int> buffer = new(0);

    Expect.AreEqual(0, buffer.Capacity);
    Expect.IsFalse(buffer.TryAdd(10));
    Expect.AreEqual(0, buffer.Count);
  }

  [Test]
  private void ClearAndReuse()
  {
    ConcurrentBuffer<int> buffer = new(2);
    buffer.TryAdd(10);
    buffer.TryAdd(20);

    buffer.Clear();

    Expect.AreEqual(0, buffer.Count);
    Expect.IsTrue(buffer.TryAdd(30));
    Expect.AreEqual(1, buffer.Count);
  }

  [Test]
  private void Enumerator()
  {
    ConcurrentBuffer<int> buffer = new(2);
    buffer.TryAdd(10);
    buffer.TryAdd(20);

    using var enumerator = buffer.GetEnumerator();
    Expect.IsTrue(enumerator.MoveNext());
    Expect.AreEqual(10, enumerator.Current);
    Expect.IsTrue(enumerator.MoveNext());
    Expect.AreEqual(20, enumerator.Current);
    Expect.IsFalse(enumerator.MoveNext());
  }

  [Test]
  private void EnumeratorInvalidatesByChange()
  {
    ConcurrentBuffer<int> buffer = new(2);
    buffer.TryAdd(10);
    using var enumerator = buffer.GetEnumerator();
    buffer.TryAdd(20);
    Expect.Throws<InvalidOperationException>(() => enumerator.MoveNext());
  }

  [Test]
  private void ConcurrentAdds()
  {
    const int Capacity = 128;
    const int Attempts = Capacity * 2;
    ConcurrentBuffer<int> buffer = new(Capacity);
    int added = 0;

    Parallel.For(0, Attempts, i =>
    {
      if (buffer.TryAdd(i))
      {
        Interlocked.Increment(ref added);
      }
    });

    Expect.AreEqual(Capacity, added);
    Expect.AreEqual(Capacity, buffer.Count);
    if (buffer.Count != Capacity)
      return;

    bool[] seen = new bool[Attempts];
    int enumerated = 0;
    foreach (int value in buffer)
    {
      Expect.IsTrue((uint)value < Attempts);
      if ((uint)value >= Attempts)
        continue;

      Expect.IsFalse(seen[value]);
      seen[value] = true;
      enumerated++;
    }
    Expect.AreEqual(Capacity, enumerated);
  }
}
