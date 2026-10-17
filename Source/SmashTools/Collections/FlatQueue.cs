using System;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace SmashTools.Collections;

/// <summary>
/// Fixed size queue for performance-critical FIFO behavior.
/// </summary>
[PublicAPI]
public class FlatQueue<T>
{
  private readonly T[] array;
  private int head;
  private int tail;

  /// <summary>
  /// Initializes a new instance of the <see cref="FlatQueue{T}"/> class with the specified capacity.
  /// </summary>
  /// <param name="size">Fixed size of the queue.</param>
  public FlatQueue(int size)
  {
    if (size < 0)
      throw new ArgumentOutOfRangeException(nameof(size));

    array = new T[size];
  }

  /// <summary>
  /// Gets the size of the queue.
  /// </summary>
  public int Size => array.Length;

  /// <summary>
  /// Gets the count of elements in the queue.
  /// </summary>
  public int Count => head - tail;

  /// <summary>
  /// Gets the element at the specified logical index, accounting for wrap-around.
  /// </summary>
  /// <param name="index">The zero-based logical index within the buffer.</param>
  /// <returns>The element at the given logical position.</returns>
  public T this[int index] => array[index];

  /// <summary>
  /// Resets queue accessors to front.
  /// </summary>
  /// <remarks>
  /// <para>
  /// Does NOT clear the queue, only resets indexers to allow existing elements to be overwritten.
  /// This is for fast clearing where the queue will overwrite existing values without having to clear the old ones.
  /// Use <see cref="Clear"/> to remove all items from the queue.
  /// </para>
  /// </remarks>
  public void Reset()
  {
    head = 0;
    tail = 0;
  }

  /// <summary>
  /// Clear all items from queue and reset indexers.
  /// </summary>
  public void Clear()
  {
    for (int i = 0; i < array.Length; i++)
    {
      array[i] = default!;
    }
    Reset();
  }

  /// <summary>
  /// Adds an item to the head of the buffer, overwriting the oldest element if the buffer is full.
  /// </summary>
  /// <param name="item">The item to push into the buffer.</param>
  public void Enqueue(T item)
  {
    array[head++] = item;
    Assert.IsFalse(head > Size);
  }

  /// <summary>
  /// Removes the next element in queue without shifting elements.
  /// </summary>
  /// <returns>The next element in the queue.</returns>
  public T Dequeue()
  {
    T item = array[tail];
    array[tail] = default!;
    tail++;
    return item;
  }
}