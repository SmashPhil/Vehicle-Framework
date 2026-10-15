using System;
using JetBrains.Annotations;

namespace CoreLib.Performance;

/// <summary>
/// Object pool that reuses instances of <typeparamref name="T"/> to reduce allocations and construction overhead.
/// </summary>
/// <typeparam name="T">Type of object managed by the pool.</typeparam>
[PublicAPI]
public interface IObjectPool<T>
{
  /// <summary>
  /// Returns an item back to the pool so it can be reused by future <see cref="Get"/> calls.
  /// </summary>
  /// <param name="item">The item to return to the pool.</param>
  /// <remarks>
  /// Implementations may reject <paramref name="item"/> (for example when the pool is at capacity),
  /// ignore duplicate returns, or validate the item before accepting it.
  /// </remarks>
  void Return(T item);

  /// <summary>
  /// Gets an item from the pool.
  /// </summary>
  /// <returns>
  /// An instance of <typeparamref name="T"/>.
  /// </returns>
  T Get();

  /// <summary>
  /// Removes all currently pooled items, resetting the pool to an empty state.
  /// </summary>
  void Clear();
}

/// <summary>
/// Extends <see cref="IObjectPool{T}"/> with a scoped acquisition API.
/// </summary>
/// <typeparam name="T">Type of object managed by the pool.</typeparam>
/// <typeparam name="TScope">
/// A disposable struct that owns the temporary checkout lifetime. Disposing the scope should return the object
/// to the pool.
/// </typeparam>
/// <remarks>
/// The <typeparamref name="TScope"/> value ties the lifetime of the checked-out object to a disposable struct.
/// When the struct is disposed, the object should be returned to the pool.
/// </remarks>
[PublicAPI]
public interface IObjectPool<T, out TScope> : IObjectPool<T> where TScope : struct, IDisposable
{
  /// <summary>
  /// Temporarily checks out an item from the pool and returns a scope that will return it to the pool when disposed.
  /// </summary>
  /// <param name="obj">The checked-out object instance.</param>
  /// <returns>
  /// A disposable scope that controls the lifetime of <paramref name="obj"/> outside of the pool.
  /// Disposing the returned scope returns <paramref name="obj"/> to the pool.
  /// </returns>
  /// <remarks>
  /// Intended usage is <see langword="using"/> / <see langword="using"/> <see langword="var"/> to ensure the object
  /// is always returned to the pool.
  /// </remarks>
  TScope GetTemporary(out T obj);
}
