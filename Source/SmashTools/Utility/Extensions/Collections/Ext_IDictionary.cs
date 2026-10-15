using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace SmashTools;

[PublicAPI]
public static class Ext_IDictionary
{
  /// <summary>
  /// Add <paramref name="value"/> as new entry into <paramref name="dictionary"/> or insert into existing list
  /// </summary>
  /// <typeparam name="K"></typeparam>
  /// <typeparam name="C"></typeparam>
  /// <typeparam name="V"></typeparam>
  /// <param name="dictionary"></param>
  /// <param name="key"></param>
  /// <param name="value"></param>
  public static void AddOrAppend<K, C, V>(this IDictionary<K, C> dictionary, K key,
    V value) where C : ICollection<V>, new()
  {
    if (dictionary == null)
      throw new ArgumentNullException(nameof(dictionary), "Dictionary cannot be null.");
    if (key == null)
      throw new ArgumentNullException(nameof(key), "Key cannot be null.");

    if (!dictionary.ContainsKey(key))
      dictionary.Add(key, []);
    dictionary[key].Add(value);
  }
}