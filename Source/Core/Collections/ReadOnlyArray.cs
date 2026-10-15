using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace CoreLib.Collections;

[PublicAPI]
public readonly struct ReadOnlyArray<T>(T[] array) : IEnumerable<T>
{
  public int Length => array.Length;

  public T this[int index] => array[index];

  public Enumerator GetEnumerator()
  {
    return new Enumerator(array);
  }

  IEnumerator<T> IEnumerable<T>.GetEnumerator()
  {
    return new Enumerator(array);
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return new Enumerator(array);
  }

  public struct Enumerator : IEnumerator<T>
  {
    private readonly T[] array;
    private int index = -1;

    internal Enumerator(T[] array)
    {
      this.array = array;
    }

    public T Current => array[index];

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
      int next = index + 1;
      if (next >= array.Length)
        return false;

      // Advane current index only if it's within the array bounds, otherwise Current will throw
      index = next;
      return true;
    }

    public void Reset()
    {
      index = -1;
    }

    public void Dispose()
    {
    }
  }
}