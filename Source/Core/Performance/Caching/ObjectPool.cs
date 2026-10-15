using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace CoreLib.Performance;

/// <summary>
/// Generic object pool for retaining object lifetime beyond its intended scope.
/// </summary>
/// <remarks>
/// If <typeparamref name="T"/> implements <see cref="IDisposable"/>, <see cref="Clear"/> must be called at the end
/// of the object pool's lifetime to ensure all remaining objects in the pool are disposed.
/// </remarks>
/// <typeparam name="T"></typeparam>
[PublicAPI]
[DebuggerDisplay("Count = {Count}")]
public class ObjectPool<T> : IObjectPool<T, ObjectPool<T>.Scope>
{
  // Raw stack implementation for fast retrieval and insertion with no auto-resizing.
  private T[] pool;
  private int head = -1;

  private readonly Func<T> factory;

  private readonly object poolLock = new();

  /// <summary>
  /// Creates fixed size object pool
  /// </summary>
  /// <param name="size">
  /// Size of object pool. When ObjectPool hits max count, 
  /// additional objects will not be accepted.
  /// </param>
  public ObjectPool(int size)
  {
    var ctor = typeof(T).GetConstructor(Type.EmptyTypes);
    if (ctor == null)
      throw new ArgumentException("ObjectPool with no factory method must contain parameterless constructor for type T.");

    pool = new T[size];
    factory = MethodBuilder.GetConstructor<T>(ctor);
  }

  /// <summary>
  /// Creates fixed size object pool
  /// </summary>
  /// <param name="size">
  /// Size of object pool. When ObjectPool hits max count, 
  /// additional objects will not be accepted.
  /// </param>
  /// <param name="factory">
  /// Factory method for creating new instances of <typeparam name="T"/>
  /// when new object needs to be instantiated for pooling.
  /// </param>
  public ObjectPool(int size, Func<T> factory)
  {
    pool = new T[size];
    this.factory = factory;
  }

  /// <summary>
  /// Creates fixed size object pool
  /// </summary>
  /// <param name="size">
  /// Size of object pool. When ObjectPool hits max count, 
  /// additional objects will not be accepted.
  /// </param>
  /// <param name="preWarm">
  /// Add new objects to pool upon initialization.
  /// </param>
  public ObjectPool(int size, int preWarm) : this(size)
  {
    PreWarm(preWarm);
  }

  /// <summary>
  /// Creates fixed size object pool
  /// </summary>
  /// <param name="size">
  /// Size of object pool. When ObjectPool hits max count, 
  /// additional objects will not be accepted.
  /// </param>
  /// <param name="preWarm">
  /// Add new objects to pool upon initialization.
  /// </param>
  /// <param name="factory">
  /// Factory method for creating new instances of <typeparam name="T"/>
  /// when new object needs to be instantiated for pooling.
  /// </param>
  public ObjectPool(int size, int preWarm, Func<T> factory) : this(size, factory)
  {
    PreWarm(preWarm);
  }

  /// <summary>
  /// Count of objects inside the pool.
  /// </summary>
  // NOTE - this is just for unit testing and debugging, the warnings here are
  // unwarranted as long as the context in which this is used does not change.
  // ReSharper disable once ConvertToAutoProperty
  // ReSharper disable once InconsistentlySynchronizedField
  public int Count => head + 1;

  /// <summary>
  /// Size of pool.
  /// </summary>
  public int Size => pool.Length;

  /// <summary>
  /// ObjectPool can grow dynamically when more items are returned than its current capacity.
  /// </summary>
  public bool Resizable { get; set; }

  /// <summary>
  /// Growth factor when resizing pool.
  /// </summary>
  public int GrowthFactor { get; set; } = 2;

  /// <summary>
  /// Add <paramref name="item"/> to pool.
  /// </summary>
  /// <remarks>
  /// If pool has hit capacity, item will be discarded.
  /// </remarks>
  public void Return(T item)
  {
    lock (poolLock)
    {
      if (head >= pool.Length - 1)
      {
        if (!Resizable)
        {
          // NOTE - there are no guarantees that T will never inherit from IDisposable as this is
          // a public api. CoreLib.Burst.PathFinder does pool disposable objects.
          (item as IDisposable)?.Dispose();
          return;
        }
        Array.Resize(ref pool, newSize: pool.Length * GrowthFactor);
      }

      if (item is IPoolable poolable)
      {
        poolable.Reset();
        poolable.InPool = true;
      }
      pool[++head] = item;
    }
  }

  /// <summary>
  /// Get object from pool. If pool is empty, a new object will be created and returned instead.
  /// </summary>
  public T Get()
  {
    T item;
    lock (poolLock)
    {
      if (head == -1)
      {
        item = factory();
      }
      else
      {
        item = pool[head];
        pool[head] = default;
        head--;
      }
    }

    if (item is IPoolable poolable)
    {
      poolable.InPool = false;
    }
    return item;
  }

  /// <summary>
  /// Try to get existing item from pool. If pool is empty,
  /// </summary>
  /// <param name="item"></param>
  /// <returns>
  /// <see langword="false"/> if pool is empty. <see langword="true"/> if an item is available for fetching.
  /// </returns>
  public bool TryGet(out T item)
  {
    lock (poolLock)
    {
      item = default;
      if (head == -1)
        return false;

      item = Get();
      return true;
    }
  }

  /// <summary>
  /// Get object from pool scoped to <see cref="Scope"/> object's lifetime.
  /// </summary>
  /// <remarks>Returns to pool when the <see cref="Scope"/> object is disposed.</remarks>
  /// <param name="obj">Object acquired from pool.</param>
  /// <returns>Disposable <see cref="Scope"/> object.</returns>
  [MustDisposeResource]
  public Scope GetTemporary(out T obj)
  {
    obj = Get();
    return new Scope(this, obj);
  }

  /// <summary>
  /// Ensure objects already populate the object pool. If object pool count does not 
  /// exceed <paramref name="count"/>, new objects will be created to populate the pool.
  /// </summary>
  /// <remarks>
  /// If pooled objects already exist from 0 to <paramref name="count"/>, 
  /// no new objects will be added.
  /// </remarks>
  public void PreWarm(int count)
  {
    lock (poolLock)
    {
      int countToAdd = count - Count;
      if (countToAdd > 0)
      {
        for (int i = 0; i < countToAdd; i++)
        {
          Return(factory());
        }
      }
    }
  }

  /// <summary>
  /// Remove all objects from pool and reset head to 0.
  /// </summary>
  /// <remarks>
  /// If the object is disposable, Dispose will be called after its removal from the pool.
  /// </remarks>
  public void Clear()
  {
    lock (poolLock)
    {
      while (Count > 0)
      {
        T item = Get();
        (item as IDisposable)?.Dispose();
      }
      Assert.AreEqual(Count, 0);
    }
  }

  /// <summary>
  /// Scoped acquisition of ObjectPool object.
  /// </summary>
  [PublicAPI]
  public readonly struct Scope : IDisposable
  {
    private readonly ObjectPool<T> pool;
    private readonly T item;

    internal Scope(ObjectPool<T> pool, in T item)
    {
      this.pool = pool;
      this.item = item;
    }

    void IDisposable.Dispose()
    {
      pool.Return(item);
    }
  }
}