using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace CoreLib.Collections;

[PublicAPI]
public static class Ext_IEnumerable
{
  extension<T>(IEnumerable<T> enumerable)
  {
    /// <summary>
    /// Returns a flattened sequence by recursively projecting each element of the source sequence to an enumerable and
    /// concatenating the results.
    /// </summary>
    /// <remarks>
    /// This method performs a recursive traversal, yielding each element and all elements returned by the selector
    /// function applied to each element. The traversal is depth-first, and the order of elements in the
    /// result reflects this.
    /// </remarks>
    /// <param name="selector">
    /// A transform function supplying the elements to recurse on from the current enumerated object.
    /// </param>
    /// <returns>
    /// An enumerable containing the original elements and all recursively selected child elements in depth-first
    /// order.
    /// </returns>
    public IEnumerable<T> SelectManyAndFlatten(Func<T, IEnumerable<T>> selector)
    {
      foreach (T item in enumerable)
      {
        yield return item;

        foreach (T item2 in selector(item).SelectManyAndFlatten(selector))
        {
          yield return item2;
        }
      }
    }
  }
}
