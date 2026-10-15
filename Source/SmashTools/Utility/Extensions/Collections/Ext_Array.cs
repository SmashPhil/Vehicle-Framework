using System;

namespace SmashTools;

public static class Ext_Array
{
  /// <summary>
  /// Populate entirety of <paramref name="array"/> with <paramref name="value"/>
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="array"></param>
  /// <param name="value"></param>
  public static T[] Populate<T>(this T[] array, T value)
  {
    for (int i = 0; i < array.Length; ++i)
      array[i] = value;
    return array;
  }

  /// <summary>
  /// Populate entirety of <paramref name="array"/> while retrieving default value from <paramref name="valueGetter"/>. Useful for instantiating array of lists
  /// </summary>
  /// <typeparam name="T"></typeparam>
  /// <param name="array"></param>
  /// <param name="valueGetter"></param>
  public static T[] Populate<T>(this T[] array, Func<T> valueGetter)
  {
    for (int i = 0; i < array.Length; ++i)
      array[i] = valueGetter();
    return array;
  }
}