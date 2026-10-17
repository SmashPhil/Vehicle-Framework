using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace CoreLib.Collections;

[PublicAPI]
public readonly struct ReadOnlyList<T>(List<T> list) : IList<T>, IReadOnlyList<T>
{
  public int Count => list.Count;

  public bool IsReadOnly => true;

  public bool IsFixedSize => false;

  public bool IsSynchronized => false;

  public T this[int index]
  {
    get => list[index];
    set => throw new NotSupportedException();
  }

  IEnumerator<T> IEnumerable<T>.GetEnumerator()
  {
    return list.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return list.GetEnumerator();
  }

  public List<T>.Enumerator GetEnumerator()
  {
    return list.GetEnumerator();
  }

  public int IndexOf(T item)
  {
    return list.IndexOf(item);
  }

  public void Insert(int index, T item)
  {
    throw new NotSupportedException();
  }

  public void RemoveAt(int index)
  {
    throw new NotSupportedException();
  }

  public void Add(T item)
  {
    throw new NotSupportedException();
  }

  public bool Remove(T item)
  {
    throw new NotSupportedException();
  }

  public void Clear()
  {
    throw new NotSupportedException();
  }

  public bool Contains(T item)
  {
    return list.Contains(item);
  }

  public void CopyTo(T[] array, int arrayIndex)
  {
    list.CopyTo(array, arrayIndex);
  }
}