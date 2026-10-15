using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace CoreLib.Performance;

/// <summary>
/// Copy contents of list and store in temporary object for enumeration.
/// </summary>
/// <remarks>List snapshot will be returned to object pool upon disposal.</remarks>
[PublicAPI]
public readonly struct ListSnapshot<T> : IDisposable, IEnumerable<T>
{
  public readonly List<T> items;

  public ListSnapshot(List<T> listToCopy)
  {
    items = AsyncPool<List<T>>.Get();
    items.AddRange(listToCopy);
  }

  public int Count => items.Count;

  void IDisposable.Dispose()
  {
    items.Clear();
    AsyncPool<List<T>>.Return(items);
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return items.GetEnumerator();
  }

  IEnumerator<T> IEnumerable<T>.GetEnumerator()
  {
    return items.GetEnumerator();
  }
}