using System;
using JetBrains.Annotations;

namespace CoreLib.PathFinding;

/// <summary>
/// Represents an async pathfinding operation that can be queried for completion,
/// resolved to a <see cref="Path"/>, or canceled.
/// </summary>
[PublicAPI]
public interface IPathPromise : IDisposable
{
  /// <summary>
  /// Gets a value indicating whether the pathfinding operation has completed.
  /// </summary>
  bool IsCompleted { get; }

  /// <summary>
  /// Gets the resolved <see cref="Path"/> for this operation.
  /// </summary>
  /// <returns>The completed path.</returns>
  Path GetPath();

  /// <summary>
  /// Cancels the pathfinding operation.
  /// </summary>
  void Cancel();
}