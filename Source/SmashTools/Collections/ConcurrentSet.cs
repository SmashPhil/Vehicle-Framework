using System.Collections.Concurrent;

namespace SmashTools
{
  /// <summary>
  /// Wrapper collection for storing a hash table of items with shorthand methods for adding and
  /// removing items.
  /// </summary>
  /// <remarks>
  /// This is just for convenience, and the value type of the dictionary is the smallest
  /// possible data type to mitigate overhead.
  /// </remarks>
  public class ConcurrentSet<T> : ConcurrentDictionary<T, byte>
  {
    public bool Add(T item)
    {
      return TryAdd(item, 0);
    }

    public bool Remove(T item)
    {
      return TryRemove(item, out _);
    }

    public bool Contains(T item)
    {
      return ContainsKey(item);
    }
  }
}