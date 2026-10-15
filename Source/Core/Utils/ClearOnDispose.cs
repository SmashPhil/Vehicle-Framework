using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace CoreLib;

/// <summary>
/// Clears the collection when this struct goes out of scope.
/// </summary>
/// <typeparam name="T">Type of the collection being rolled back.</typeparam>
[PublicAPI]
public readonly struct ClearOnDispose<T>(ICollection<T> collection) : IDisposable
{
  void IDisposable.Dispose()
  {
    collection.Clear();
  }
}