using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace CoreLib.Performance;

// TODO 1.8 - Move to Collections.Concurrent
/// <summary>
/// Copy contents of list and store in temporary object for enumeration.
/// </summary>
/// <remarks>List snapshot will be returned to object pool upon disposal.</remarks>
[PublicAPI]
public readonly struct ListSnapshot<T> : IDisposable, IEnumerable<T>
{
  public readonly List<T> items;

  public ListSnapshot([NotNull] List<T> listToCopy)
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

  public List<T>.Enumerator GetEnumerator()
  {
    return items.GetEnumerator();
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  IEnumerator<T> IEnumerable<T>.GetEnumerator()
  {
    return GetEnumerator();
  }
}