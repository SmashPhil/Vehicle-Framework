using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using JetBrains.Annotations;

namespace CoreLib.Collections.Concurrent;

/// <summary>
/// A fixed-capacity buffer that supports concurrent additions.
/// </summary>
/// <typeparam name="T">The type of item stored.</typeparam>
[PublicAPI]
public sealed class ConcurrentBuffer<T> : IEnumerable<T>
{
  private int version;
  private int head;
  private readonly T[] items;

  /// <summary>
  /// Creates a buffer with the given capacity.
  /// </summary>
  /// <param name="size">The maximum number of items.</param>
  public ConcurrentBuffer(int size)
  {
    items = new T[size];
  }

  /// <summary>
  /// Gets the number of items in the buffer.
  /// </summary>
  public int Count => head;

  /// <summary>
  /// Gets the maximum number of items the buffer can hold.
  /// </summary>
  public int Capacity => items.Length;

  /// <summary>
  /// Attempts to add an item to the buffer.
  /// </summary>
  /// <param name="item">The item to add.</param>
  /// <returns><see langword="true"/> if added; <see langword="false"/> if the buffer is full.</returns>
  public bool TryAdd(T item)
  {
    Interlocked.Increment(ref version);
    int index;
    do
    {
      index = Volatile.Read(ref head);
      if (index >= Capacity)
        return false;
    } while (Interlocked.CompareExchange(ref head, index + 1, index) != index);

    items[index] = item;
    return true;
  }

  /// <summary>
  /// Removes all items. Call only when no other operation is using the buffer.
  /// </summary>
  public void Clear()
  {
    Interlocked.Increment(ref version);
    head = 0;
    Array.Clear(items, 0, items.Length);
  }

  /// <summary>
  /// Returns an enumerator over the items.
  /// </summary>
  public Enumerator GetEnumerator()
  {
    return new Enumerator(this);
  }

  IEnumerator<T> IEnumerable<T>.GetEnumerator()
  {
    return GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  public struct Enumerator : IEnumerator<T>
  {
    private readonly ConcurrentBuffer<T> buffer;
    private readonly int version;

    private int index;

    internal Enumerator(ConcurrentBuffer<T> buffer)
    {
      this.buffer = buffer;
      version = buffer.version;
    }

    private void ThrowIfMismatchedVersion()
    {
      if (version != buffer.version)
      {
        throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
      }
    }

    public void Dispose()
    {
    }

    public bool MoveNext()
    {
      ThrowIfMismatchedVersion();
      if ((uint)index < (uint)buffer.Count)
      {
        Current = buffer.items[index];
        index++;
        return true;
      }

      Current = default;
      index = -1;
      return false;
    }

    public T Current { get; private set; }

    object IEnumerator.Current
    {
      get
      {
        if (index <= 0)
          throw new InvalidOperationException("Current is unassigned until the enumerator has moved next once.");

        return Current;
      }
    }

    void IEnumerator.Reset()
    {
      ThrowIfMismatchedVersion();
      index = 0;
      Current = default;
    }
  }
}
