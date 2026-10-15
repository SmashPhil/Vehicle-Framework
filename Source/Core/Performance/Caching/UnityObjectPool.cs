using System;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace CoreLib.Performance;

[PublicAPI]
[DebuggerDisplay("Count = {Count}")]
public class UnityObjectPool<T> : IDisposable, IObjectPool<T> where T : Object
{
  // Raw stack implementation for fast retrieval and insertion
  // with no auto-resizing.
  private readonly T[] pool;
  private int head = -1;

  private Func<T> factory;
  private Action<T> onDestroy;

  /// <summary>
  /// Creates fixed size object pool
  /// </summary>
  /// <param name="factory">
  /// Factory method for constructing objects when ObjectPool is empty
  /// </param>
  /// <param name="onDestroy">
  /// Pre-destruction event hook, primarily for any additional resource handling
  /// that may be required such as <see cref="RenderTexture.Release"/>
  /// </param>
  /// <param name="size">
  /// Size of object pool. When ObjectPool hits max count, 
  /// additional objects will not be accepted.
  /// </param>
  /// <param name="preWarm">
  /// Add new objects to pool upon initialization.
  /// </param>
  public UnityObjectPool(Func<T> factory, int size, Action<T> onDestroy = null, int preWarm = 0)
  {
    this.factory = factory;
    this.onDestroy = onDestroy;
    pool = new T[size];
    PreWarm(preWarm);
  }

  /// <summary>
  /// Head of internal stack
  /// </summary>
  // NOTE - this is just for unit testing and debugging, the warnings here are unwarranted.
  // ReSharper disable once ConvertToAutoProperty
  // ReSharper disable once InconsistentlySynchronizedField
  public int Count => head + 1;

  /// <summary>
  /// Add <paramref name="item"/> to pool.
  /// </summary>
  public void Return(T item)
  {
    if (head >= pool.Length - 1)
    {
      onDestroy?.Invoke(item);
      Object.Destroy(item);
      return;
    }
    pool[++head] = item;
  }

  /// <summary>
  /// Get object from pool. If pool is empty, a new object will be created and returned instead.
  /// </summary>
  public T Get()
  {
    if (head == -1)
      return factory();

    T item = pool[head];
    pool[head] = null;
    head--;
    return item;
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
    int countToAdd = count - Count;
    if (countToAdd > 0)
    {
      for (int i = 0; i < countToAdd; i++)
      {
        Return(factory());
      }
    }
  }

  /// <summary>
  /// Remove all objects from pool and reset head to 0.
  /// </summary>
  public void Clear()
  {
    while (Count > 0)
    {
      T obj = Get();
      onDestroy?.Invoke(obj);
      Object.Destroy(obj);
    }
    Assert.AreEqual(Count, 0);
    Assert.IsNull(pool[0]);
  }

  public void Dispose()
  {
    Clear();
  }
}