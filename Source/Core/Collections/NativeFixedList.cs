#if ENABLE_UNITY_COLLECTIONS_CHECKS
#define ENABLE_BOUNDS_CHECK
#endif

using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

// NOTE - Using Hungarian notation since Unity expects certain fields to be spelled exactly in that manner
// otherwise it will spit out errors for NativeContainer.
// ReSharper disable InconsistentNaming

namespace CoreLib.Collections;

[PublicAPI, NativeContainer]
[DebuggerDisplay("Count = {Count}")]
public unsafe struct NativeFixedList<T> : IDisposable where T : unmanaged
{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
    private AtomicSafetyHandle m_Safety;
    [NativeSetClassTypeToNullOnSchedule]
    private DisposeSentinel m_DisposeSentinel;
#endif

  [NativeDisableUnsafePtrRestriction]
  private T* m_Buffer;
  [NativeDisableUnsafePtrRestriction]
  private int* m_Count;

  private Allocator m_Allocator;

  public NativeFixedList(int length, Allocator allocator)
  {
    if (length <= 0)
      throw new ArgumentException("length must be greater than 0.");

    m_Allocator = allocator;
    m_Count = (int*)UnsafeUtility.Malloc(
      size: UnsafeUtility.SizeOf<int>(),
      alignment: UnsafeUtility.AlignOf<int>(), allocator);

    m_Buffer = (T*)UnsafeUtility.Malloc(
      size: (long)UnsafeUtility.SizeOf<T>() * length,
      alignment: UnsafeUtility.AlignOf<T>(), allocator);

    *m_Count = 0;
    Length = length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, callSiteStackDepth: 1, allocator);
#endif
  }

  /// <summary>
  /// Container has allocated buffers and is safe to use.
  /// </summary>
  public readonly bool IsCreated => m_Buffer != null && m_Count != null;

  /// <summary>
  /// Total capacity of the list.
  /// </summary>
  public int Length { get; }

  /// <summary>
  /// Total number of items stored in the list.
  /// </summary>
  public readonly int Count => IsCreated ? *m_Count : 0;

  public T this[int index]
  {
    get
    {
#if ENABLE_BOUNDS_CHECK
        if (index >= Count)
          throw new IndexOutOfRangeException(nameof(index));
#endif
      return m_Buffer[index];
    }
  }

  public void Add(T item)
  {
#if ENABLE_BOUNDS_CHECK
      if (Count == Length)
        throw new IndexOutOfRangeException("Adding item to fixed size list with no more space available.");
#endif

    m_Buffer[*m_Count] = item;
    *m_Count += 1;
  }

  public T Remove()
  {
#if ENABLE_BOUNDS_CHECK
      if (Count == 0)
        throw new IndexOutOfRangeException("Removing item from empty list.");
#endif

    *m_Count -= 1;
    return m_Buffer[*m_Count];
  }

  public void Clear()
  {
    *m_Count = 0;
  }

  public void Dispose()
  {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if (!UnsafeUtility.IsValidAllocator(m_Allocator))
        throw new InvalidOperationException("Trying to dispose an object with an invalid allocator.");

      DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

    Assert.IsTrue(m_Buffer != null);
    UnsafeUtility.Free(m_Buffer, m_Allocator);
    m_Buffer = null;

    Assert.IsTrue(m_Count != null);
    UnsafeUtility.Free(m_Count, m_Allocator);
    m_Count = null;

    m_Allocator = Allocator.Invalid;
  }
}
