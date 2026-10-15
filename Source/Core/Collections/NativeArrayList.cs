using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

// NOTE - Using Hungarian notation since Unity expects certain fields to be spelled exactly in that manner
// otherwise it will spit out errors for NativeContainer.
// ReSharper disable InconsistentNaming

namespace CoreLib.Collections;

[DebuggerDisplay("Size = {Size}")]
public unsafe struct NativeArrayList<T> : INativeDisposable where T : unmanaged
{
  private NativeArray<BufferItem> m_Buffer;
  private Allocator m_AllocatorLabel;
  private readonly int m_Length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    private AtomicSafetyHandle m_Safety;
    [NativeSetClassTypeToNullOnSchedule]
    private DisposeSentinel m_DisposeSentinel;
#endif

  public struct BufferItem
  {
    [NativeDisableUnsafePtrRestriction]
    public T* m_ArrayBlock;
  }

  private NativeArrayList(int size, int arrayLength, Allocator allocator)
  {
    if (size == 0)
      throw new ArgumentException("Cannot allocate 0 size buffer.");

    m_AllocatorLabel = allocator;
    m_Buffer = new NativeArray<BufferItem>(size, allocator);
    m_Length = arrayLength;
    Size = size;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, callSiteStackDepth: 1, allocator);
#endif
  }

  public int Size { get; }

  public bool IsCreated => m_Buffer.IsCreated;

  public T* this[int bufferIndex]
  {
    get
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (!m_Buffer.IsCreated)
          throw new InvalidOperationException("Accessing buffer table which was freed.");

        if (bufferIndex < 0 || bufferIndex >= m_Buffer.Length)
          throw new ArgumentOutOfRangeException(nameof(bufferIndex));
#endif
      return m_Buffer[bufferIndex].m_ArrayBlock;
    }
  }

  public T this[int bufferIndex, int index]
  {
    get
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (!m_Buffer.IsCreated)
          throw new InvalidOperationException("Accessing buffer table which was freed.");

        if (bufferIndex < 0 || bufferIndex >= m_Buffer.Length)
          throw new ArgumentOutOfRangeException(nameof(bufferIndex));
#endif

      BufferItem buffer = m_Buffer[bufferIndex];
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if (index < 0 || index >= m_Length)
          throw new ArgumentOutOfRangeException(nameof(index));
#endif
      return buffer.m_ArrayBlock[index];
    }
  }

  [WriteAccessRequired]
  public void Dispose()
  {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if (m_AllocatorLabel != Allocator.None && !AtomicSafetyHandle.IsDefaultValue(in m_Safety))
        AtomicSafetyHandle.CheckExistsAndThrow(in m_Safety);
#endif

    if (!IsCreated)
      return;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if (m_AllocatorLabel == Allocator.Invalid)
        throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");

      if (!UnsafeUtility.IsValidAllocator(m_AllocatorLabel))
        throw new InvalidOperationException("Trying to dispose an object with an invalid allocator.");

      DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

    m_Buffer.Dispose();
    m_Buffer = default;
    m_AllocatorLabel = Allocator.Invalid;
  }

  public JobHandle Dispose(JobHandle inputDeps)
  {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if (m_AllocatorLabel != Allocator.None && !AtomicSafetyHandle.IsDefaultValue(in m_Safety))
      {
        AtomicSafetyHandle.CheckExistsAndThrow(in m_Safety);
      }
#endif

    if (!IsCreated)
      return inputDeps;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if (m_AllocatorLabel >= Allocator.FirstUserIndex)
        throw new InvalidOperationException("The NativeArray can not be Disposed because it was allocated with a custom allocator, use CollectionHelper.Dispose in com.unity.collections package.");
      if (m_AllocatorLabel == Allocator.Invalid)
        throw new InvalidOperationException("The NativeArray can not be Disposed because it was not allocated with a valid allocator.");
      if (!UnsafeUtility.IsValidAllocator(m_AllocatorLabel))
        throw new InvalidOperationException("Trying to dispose an object with an invalid allocator.");

      DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

    JobHandle handle = m_Buffer.Dispose(inputDeps);
    m_Buffer = default;
    m_AllocatorLabel = Allocator.Invalid;
    return handle;
  }

  [BurstDiscard]
  private void SetPtrToBuffer(int index, T* ptr)
  {
    m_Buffer[index] = new BufferItem
    {
      m_ArrayBlock = ptr
    };
  }

  public static NativeArrayList<T> PackArrayList(NativeArray<T>.ReadOnly[] arrays, Allocator allocator)
  {
    ArgumentNullException.ThrowIfNull(arrays);
    if (arrays.Length == 0)
      throw new ArgumentException("List of arrays is null or empty.");

    NativeArrayList<T> list = new(arrays.Length, arrays[0].Length, allocator);
    for (int i = 0; i < arrays.Length; i++)
    {
      NativeArray<T>.ReadOnly array = arrays[i];
      if (array.Length != list.m_Length)
        throw new ArgumentException("All arrays must be of uniform length");

      list.SetPtrToBuffer(i, (T*)array.GetUnsafeReadOnlyPtr());
    }
    return list;
  }

  public static NativeArrayList<ulong> PackBitArrayList(NativeBitArraySlim.ReadOnly[] arrays, Allocator allocator)
  {
    ArgumentNullException.ThrowIfNull(arrays);
    if (arrays.Length == 0)
      throw new ArgumentException("List of arrays is null or empty.");

    NativeArrayList<ulong> list = new(arrays.Length, arrays[0].Length, allocator);
    for (int i = 0; i < arrays.Length; i++)
    {
      NativeBitArraySlim.ReadOnly array = arrays[i];
      list.SetPtrToBuffer(i, array.GetUnsafeReadOnlyPtr());
    }
    return list;
  }
}
