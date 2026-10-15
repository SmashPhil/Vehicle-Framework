using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace SmashTools;

/// <summary>
/// Self ordering list that pushes most frequently accessed items to the top of the list. 
/// Increases performance by decreasing frequency of worst case scenarios.
/// </summary>
[PublicAPI]
public sealed class SelfOrderingList<T> : IList<T>, IReadOnlyList<T>
{
  private const int DefaultSize = 3;

  private static readonly T[] EmptyContents = [];

  // ReSharper disable once StaticMemberInGenericType
  private static readonly uint[] EmptyCounters = [];

  private T[] contents;
  private uint[] counters;

  private int size;

  private int version;

  private EqualityComparer<T> comparer = EqualityComparer<T>.Default;

  public SelfOrderingList()
  {
    contents = EmptyContents;
    counters = EmptyCounters;
  }

  public SelfOrderingList(int capacity)
  {
    if (capacity < 0)
      throw new ArgumentOutOfRangeException(nameof(capacity),
        $"{capacity} is not within bounds of list. Capacity of list must be >= 0");

    if (capacity == 0)
    {
      contents = EmptyContents;
      counters = EmptyCounters;
    }
    else
    {
      contents = new T[capacity];
      counters = new uint[capacity];
    }
  }

  public SelfOrderingList(IEnumerable<T> enumerable)
  {
    if (enumerable is null)
      throw new ArgumentNullException(nameof(enumerable));

    if (enumerable is ICollection<T> collection)
    {
      int count = collection.Count;
      if (count == 0)
      {
        contents = EmptyContents;
        counters = EmptyCounters;
      }
      else
      {
        contents = new T[count];
        counters = new uint[count];
        collection.CopyTo(contents, 0);
        size = count;
      }
    }
    else
    {
      size = 0;
      contents = EmptyContents;
      counters = EmptyCounters;
      foreach (T item in enumerable)
      {
        Add(item);
      }
    }
  }

  bool ICollection<T>.IsReadOnly => false;

  public int Count => size;

  public int Capacity
  {
    get { return contents.Length; }
    set
    {
      if (value < size)
        throw new ArgumentOutOfRangeException(nameof(value),
          "Setting capacity lower than current item count.");

      if (value != contents.Length)
      {
        if (value > 0)
        {
          T[] newItems = new T[value];
          uint[] newCounters = new uint[value];
          if (size > 0)
          {
            Array.Copy(contents, 0, newItems, 0, size);
            Array.Copy(counters, 0, newCounters, 0, size);
          }

          contents = newItems;
          counters = newCounters;
        }
        else
        {
          contents = EmptyContents;
          counters = EmptyCounters;
        }
        version++;
      }
    }
  }

  public T this[int index]
  {
    get
    {
      return contents[index];
    }
    set
    {
      contents[index] = value;
      version++;
    }
  }

  private static bool IsCompatibleObject(object value)
  {
    return value is T || value is null && default(T) is null;
  }

  public void Add(T item)
  {
    if (size == contents.Length)
      EnsureCapacity(size + 1);

    contents[size] = item;
    counters[size] = 0;
    size++;
    version++;
  }

  public void AddRange(IEnumerable<T> enumerable)
  {
    foreach (T item in enumerable)
    {
      Add(item);
    }
  }

  public void InsertRange(int index, IEnumerable<T> enumerable)
  {
    if (enumerable is null)
      throw new ArgumentNullException(nameof(enumerable));
    if (index < 0 || index > size)
      throw new IndexOutOfRangeException(nameof(index));
    // ReSharper disable once PossibleUnintendedReferenceComparison
    if (this == enumerable)
      throw new NotSupportedException("Self insertion is not supported");

    if (enumerable is ICollection<T> collection)
    {
      int count = collection.Count;
      if (count == 0)
        return;

      EnsureCapacity(size + count);
      Array.Copy(contents, index, contents, index + count, size - index);
      Array.Copy(counters, index, counters, index + count, size - index);
      collection.CopyTo(contents, index);
      size += count;
      version++;
    }
    else
    {
      foreach (T item in enumerable)
      {
        Insert(index++, item);
      }
    }
  }

  public IEnumerator GetEnumerator()
  {
    return new Enumerator(this);
  }

  IEnumerator<T> IEnumerable<T>.GetEnumerator()
  {
    return new Enumerator(this);
  }

  public ReadOnlyCollection<T> AsReadOnly()
  {
    return new ReadOnlyCollection<T>(this);
  }

  public void Clear()
  {
    if (size > 0)
    {
      Array.Clear(contents, 0, size);
      Array.Clear(counters, 0, size);
      size = 0;
    }
    version++;
  }

  public bool Contains(T item)
  {
    // IndexOf calls Array.IndexOf, which internally calls EqualityComparer<T>.Default.IndexOf,
    // which is specialized for different types. This boosts performance since instead of making a
    // virtual method call each iteration of the loop, via EqualityComparer<T>.Default.Equals, we
    // only make one virtual call to EqualityComparer.IndexOf.
    return size > 0 && IndexOf(item) >= 0;
  }

  private void EnsureCapacity(int min)
  {
    // Array.MaxArrayLength
    const uint MaxArrayLength = 2146435071U;

    if (contents.Length < min)
    {
      int newCapacity = contents.Length == 0 ? DefaultSize : contents.Length * 2;
      if ((uint)newCapacity > MaxArrayLength)
        newCapacity = (int)MaxArrayLength;
      if (newCapacity < min)
        newCapacity = min;
      Capacity = newCapacity;
    }
  }

  public T Grab(T item)
  {
    TryGrab(item, out T result);
    return result;
  }

  public bool TryGrab(T item, out T result)
  {
    for (int i = 0; i < size; i++)
    {
      if (!comparer.Equals(contents[i], item))
        continue;
      result = contents[i];
      Touch(i);
      return true;
    }
    result = default;
    return false;
  }

  public void Touch(int index)
  {
    if (index < 0 || index >= size)
      throw new ArgumentOutOfRangeException(nameof(index));

    counters[index]++;
    if (index > 0 && counters[index] > counters[index - 1])
      Bump(index);
    if (counters[index] == uint.MaxValue)
      ResetCounters();
  }

  private void ResetCounters()
  {
    // Preserve relative ranking so items aren't suddenly bumped around after a reset
    for (int i = 0; i < size; i++)
    {
      counters[i] = (uint)(size - i);
    }
  }

  [Pure]
  public int IndexOf(T item)
  {
    return Array.IndexOf(contents, item, 0, size);
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private void Bump(int index)
  {
    // Destruction swap
    (contents[index], contents[index - 1]) = (contents[index - 1], contents[index]);
    (counters[index], counters[index - 1]) = (counters[index - 1], counters[index]);
    version++;
  }

  public void Insert(int index, T item)
  {
    if ((uint)index > (uint)size)
      throw new ArgumentOutOfRangeException(nameof(index));

    if (size == contents.Length)
      EnsureCapacity(size + 1);

    if (index < size)
      Array.Copy(contents, index, contents, index + 1, size - index);

    contents[index] = item;
    size++;
    version++;
  }

  public bool Remove(T item)
  {
    int index = IndexOf(item);
    if (index >= 0)
    {
      RemoveAt(index);
      return true;
    }

    return false;
  }

  public void RemoveAt(int index)
  {
    if (index < 0 || index >= size)
      throw new ArgumentOutOfRangeException(nameof(index));

    size--;
    Array.Copy(contents, index + 1, contents, index, size - index);
    Array.Copy(counters, index + 1, counters, index, size - index);
    contents[size] = default!;
    counters[size] = 0;
    version++;
  }

  public void CopyTo(T[] array)
  {
    CopyTo(array, 0);
  }

  public void CopyTo(T[] array, int arrayIndex)
  {
    if (array != null && array.Rank != 1)
      throw new ArgumentException("Multi-rank dimensions are not supported for SelfOrderingList",
        nameof(array));
    // Array.Copy checks for null
    Array.Copy(contents, 0, array, arrayIndex, size);
  }

  [Serializable]
  public struct Enumerator : IEnumerator<T>
  {
    private readonly SelfOrderingList<T> list;
    private int index;
    private readonly int version;
    private T current;

    internal Enumerator(SelfOrderingList<T> list)
    {
      this.list = list;
      index = 0;
      version = list.version;
      current = default;
    }

    public T Current
    {
      get { return current; }
    }

    object IEnumerator.Current
    {
      get
      {
        if (index == 0 || index == list.size + 1)
        {
          throw new InvalidOperationException();
        }

        return Current;
      }
    }

    void IEnumerator.Reset()
    {
      if (version != list.version)
      {
        throw new InvalidOperationException();
      }

      index = 0;
      current = default;
    }

    public void Dispose()
    {
    }

    public bool MoveNext()
    {
      SelfOrderingList<T> localList = list;

      if (version == localList.version && ((uint)index < (uint)localList.size))
      {
        current = localList.contents[index];
        index++;
        return true;
      }

      return MoveNextRare();
    }

    private bool MoveNextRare()
    {
      if (version != list.version)
      {
        throw new InvalidOperationException();
      }

      index = list.size + 1;
      current = default;
      return false;
    }
  }
}