using System;
using System.Buffers;
using JetBrains.Annotations;

namespace CoreLib.Performance;

/// <summary>
/// Rents an array from the shared pool and returns it when this scope is disposed.
/// </summary>
/// <typeparam name="T">Type of elements in the rented array.</typeparam>
[PublicAPI]
public readonly struct ArrayPoolScope<T> : IDisposable
{
  private readonly T[] array;
  private readonly bool clearOnReturn;

  /// <summary>
  /// Rents an array with at least the requested number of elements.
  /// </summary>
  /// <param name="minimumLength">Minimum length of the rented array.</param>
  /// <param name="array">Rented array, which may be longer than <paramref name="minimumLength"/>.</param>
  /// <param name="clearOnReturn">Whether to clear the array when returning it to the pool.</param>
  public ArrayPoolScope(int minimumLength, out T[] array, bool clearOnReturn = false)
  {
    array = ArrayPool<T>.Shared.Rent(minimumLength);
    this.array = array;
    this.clearOnReturn = clearOnReturn;
  }

  /// <summary>
  /// Returns the rented array to the shared pool.
  /// </summary>
  public void Dispose()
  {
    ArrayPool<T>.Shared.Return(array, clearOnReturn);
  }
}
