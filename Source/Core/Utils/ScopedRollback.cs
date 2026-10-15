using System;
using JetBrains.Annotations;

namespace CoreLib;

/// <summary>
/// Records the current value of a class object and rolls it back when this object goes out of scope.
/// </summary>
/// <typeparam name="T">Type of the object being rolled back.</typeparam>
[PublicAPI]
public readonly ref struct ScopedRollback<T> : IDisposable
{
  private readonly ref T reference;
  private readonly T value;

  /// <summary>
  /// Create temporary value state that rolls back to previous value when this struct goes out of scope.
  /// </summary>
  /// <param name="reference">Reference to rollback when this struct goes out of scope.</param>
  public ScopedRollback(ref T reference)
  {
    this.reference = ref reference;
    value = reference;
  }

  /// <summary>
  /// Create temporary value state that rolls back to previous value when this struct goes out of scope.
  /// </summary>
  /// <param name="reference">Reference to rollback when this struct goes out of scope.</param>
  /// <param name="value">Value to assign temporarily.</param>
  public ScopedRollback(ref T reference, T value)
  {
    this.reference = ref reference;
    this.value = reference;
    this.reference = value;
  }

  public void Dispose()
  {
    reference = value;
  }
}
