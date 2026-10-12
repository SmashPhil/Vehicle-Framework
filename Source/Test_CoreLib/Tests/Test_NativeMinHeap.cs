using CoreLib.Collections;
using DevTools.Testing;
using Unity.Collections;
using UnityEngine.Assertions;

namespace CoreLib.Tests;

[TestFixture(TestType.MainMenu)]
internal class Test_NativeMinHeap
{
  private const int MockCapacity = 8;

  [Test]
  public void Push()
  {
    using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
    Assert.AreEqual(expected: 0, minHeap.Length);
    minHeap.Push(element: 0, cost: 0);
    Assert.AreEqual(expected: 1, minHeap.Length);
  }

  [Test]
  public void Peek()
  {
    const int Element = 5;

    using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
    minHeap.Push(element: Element, cost: 0);
    Assert.AreEqual(expected: 1, minHeap.Length);
    Assert.AreEqual(expected: Element, minHeap.Peek());
  }

  [Test]
  public void Clear()
  {
    using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
    minHeap.Push(element: 0, cost: 0);
    Assert.AreEqual(expected: 1, minHeap.Length);
    minHeap.Clear();
    Assert.AreEqual(expected: 0, minHeap.Length);
  }

  [Test]
  public void Pop()
  {
    const int Element = 5;
    const int Cost = 2;

    using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
    minHeap.Push(element: Element, cost: Cost);
    Assert.AreEqual(Element, minHeap.Pop());
    Assert.AreEqual(expected: 0, minHeap.Length);
  }

  [Test]
  public void InvariantPop()
  {
    using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
    minHeap.Push(element: 1, cost: 2);
    minHeap.Push(element: 2, cost: 3);
    minHeap.Push(element: 3, cost: 4);
    for (int i = 3; i < minHeap.Capacity; i++)
    {
      Assert.AreEqual(expected: int.MaxValue, actual: minHeap.PeekCostUnsafe(i));
    }

    minHeap.Pop();
    Assert.AreEqual(expected: int.MaxValue, actual: minHeap.PeekCostUnsafe(2));
    minHeap.Pop();
    Assert.AreEqual(expected: int.MaxValue, actual: minHeap.PeekCostUnsafe(1));
    minHeap.Pop();
    Assert.AreEqual(expected: int.MaxValue, actual: minHeap.PeekCostUnsafe(0));
  }

  [Test]
  public void InvariantClear()
  {
    using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
    minHeap.Push(element: 1, cost: 2);
    minHeap.Push(element: 2, cost: 3);
    minHeap.Push(element: 3, cost: 4);
    for (int i = 3; i < minHeap.Capacity; i++)
    {
      Assert.AreEqual(expected: int.MaxValue, actual: minHeap.PeekCostUnsafe(i));
    }

    minHeap.Clear();
    for (int i = 0; i < minHeap.Allocated; i++)
    {
      Assert.AreEqual(expected: int.MaxValue, actual: minHeap.PeekCostUnsafe(i));
    }
  }

  [Test]
  public void Reuse()
  {
    const int ReuseCapacity = 25;

    using NativeMinHeap minHeap = new(capacity: ReuseCapacity, Allocator.Temp);
    minHeap.Push(element: 1, cost: 2);
    minHeap.Push(element: 2, cost: 3);
    minHeap.Push(element: 3, cost: 4);
    Assert.AreEqual(expected: 3, minHeap.Length);
    minHeap.Clear();
    Assert.AreEqual(expected: 0, minHeap.Length);

    const int StartElement = 5;
    for (int i = StartElement; i < ReuseCapacity; i++)
    {
      minHeap.Push(element: i, cost: i + 10);
    }

    Assert.AreEqual(expected: StartElement, actual: minHeap.Pop());
  }

  [Test]
  public void PushAndShift()
  {
    using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
    minHeap.Push(element: 3, cost: 3);
    Assert.AreEqual(expected: 3, minHeap.Peek());
    minHeap.Push(element: 2, cost: 2);
    Assert.AreEqual(expected: 2, minHeap.Peek());
    minHeap.Push(element: 1, cost: 1);
    Assert.AreEqual(expected: 1, minHeap.Peek());

    minHeap.Clear();
  }

  [Test]
  public void PopAndShift()
  {
    using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
    minHeap.Push(element: 1, cost: 4);
    minHeap.Push(element: 2, cost: 5);
    minHeap.Push(element: 3, cost: 6);

    Assert.AreEqual(expected: 1, minHeap.Pop());
    Assert.AreEqual(expected: 2, minHeap.Pop());
    Assert.AreEqual(expected: 3, minHeap.Pop());

    minHeap.Clear();
  }

  [Test]
  public void FillCompletely()
  {
    using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);

    for (int i = 0; i < minHeap.Capacity; i++)
    {
      minHeap.Push(element: i, cost: i + 1);
      Assert.AreEqual(expected: 0, actual: minHeap.Peek());
    }

    for (int i = 0; i < minHeap.Capacity; i++)
    {
      Assert.AreEqual(expected: i, actual: minHeap.Pop());
    }
  }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    [Test]
    public void NonIndexedElements()
    {
      using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
      Expect.Throws<IndexOutOfRangeException>(() => minHeap.Push(-1, 1));
      Expect.Throws<IndexOutOfRangeException>(() => minHeap.Push(MockCapacity + 1, 1));
    }

    [Test]
    public void PushOverCapacity()
    {
      using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
      Expect.Throws<IndexOutOfRangeException>(delegate
      {
        for (int i = 0; i < MockCapacity + 1; i++)
        {
          minHeap.Push(i, 0);
        }
      });
    }

    [Test]
    public void UpdateAtCapacity()
    {
      using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
      Expect.DoesNotThrow(delegate
      {
        for (int i = 0; i < MockCapacity; i++)
        {
          minHeap.Push(i, 5);
        }
        minHeap.Push(MockCapacity - 1, 1);
      });
    }

    [Test]
    public void PeekEmpty()
    {
      using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
      Expect.Throws<InvalidOperationException>(() => minHeap.Peek());
    }

    [Test]
    public void PopEmpty()
    {
      using NativeMinHeap minHeap = new(capacity: MockCapacity, Allocator.Temp);
      Expect.Throws<InvalidOperationException>(() => minHeap.Pop());
    }
#endif
}
