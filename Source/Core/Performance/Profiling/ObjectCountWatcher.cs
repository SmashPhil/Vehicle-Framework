using System;
using System.Diagnostics;

namespace CoreLib.Performance;

/// <summary>
/// Scope-based watcher for tracking how many instances of <typeparamref name="T"/>
/// are created while the watcher is active. Use with dispose pattern.
/// </summary>
/// <remarks>
/// This is a lightweight helper over <see cref="ObjectCounter"/>. Constructing the watcher
/// records the current count for <typeparamref name="T"/>, <see cref="Count"/> returns the
/// number created since that point, and <see cref="IDisposable.Dispose"/> ends the watch scope.
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
public readonly struct ObjectCountWatcher<T> : IDisposable
{
  /// <summary>
  /// Starts watching object creation count changes for <typeparamref name="T"/>.
  /// </summary>
  public ObjectCountWatcher()
  {
    ObjectCounter.StartWatcher<T>();
  }

  /// <summary>
  /// Gets the number of <typeparamref name="T"/> instances created since this watcher started.
  /// </summary>
  public int Count => ObjectCounter.GetWatchedCount<T>();

  /// <summary>
  /// Stops watching object creation count changes for <typeparamref name="T"/>.
  /// </summary>
  void IDisposable.Dispose()
  {
    ObjectCounter.EndWatcher<T>();
  }
}