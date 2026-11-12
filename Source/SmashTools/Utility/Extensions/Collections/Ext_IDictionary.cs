using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace SmashTools;

/// <summary>
/// Extension methods for working with dictionary entries
/// </summary>
[PublicAPI]
public static class Ext_IDictionary
{
  /// <summary>
  /// Adds <paramref name="value"/> to the collection at <paramref name="key"/>, creating
  /// the collection if the key is not present.
  /// </summary>
  /// <typeparam name="K">The dictionary key type.</typeparam>
  /// <typeparam name="C">The collection type stored in the dictionary.</typeparam>
  /// <typeparam name="V">The type of item in each collection.</typeparam>
  /// <param name="dictionary">The dictionary to update.</param>
  /// <param name="key">The key of the collection to update.</param>
  /// <param name="value">The item to add to the collection.</param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="dictionary"/> or <paramref name="key"/> is null.
  /// </exception>
  public static void AddOrAppend<K, C, V>(this IDictionary<K, C> dictionary, K key,
    V value) where C : ICollection<V>, new()
  {
    ArgumentNullException.ThrowIfNull(dictionary);
    ArgumentNullException.ThrowIfNull(key);
    if (!dictionary.TryGetValue(key, out C collection))
    {
      collection = [];
      dictionary.Add(key, collection);
    }
    collection.Add(value);
  }

  /// <summary>
  /// Gets the value at <paramref name="key"/>, or creates and inserts a new value if the
  /// key is not present.
  /// </summary>
  /// <typeparam name="K">The dictionary key type.</typeparam>
  /// <typeparam name="V">The value type to create when the key is absent.</typeparam>
  /// <param name="dictionary">The dictionary to search or update.</param>
  /// <param name="key">The key to look up.</param>
  /// <returns>The existing or newly created value.</returns>
  public static V GetOrInsert<K, V>(this IDictionary<K, V> dictionary, K key) where V : new()
  {
    if (!dictionary.TryGetValue(key, out V value))
    {
      value = new V();
      dictionary[key] = value;
    }
    return value;
  }
}
