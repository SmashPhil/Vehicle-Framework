
using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

// NOTE - Using Hungarian notation since Unity expects certain fields to be spelled exactly in that manner
// otherwise it will spit out errors for NativeContainer.
// ReSharper disable InconsistentNaming

namespace CoreLib.Collections;

/// <summary>
/// A minimal implementation of <see cref="NativeBitArray"/>, focusing on Set and IsSet functionality.
/// </summary>
[NativeContainer, PublicAPI]
[DebuggerDisplay("Length = {Length}")]
public unsafe struct NativeBitArraySlim : INativeDisposable
{
#if ENABLE_UNITY_COLLECTIONS_CHECKS
    private AtomicSafetyHandle m_Safety;
    [NativeSetClassTypeToNullOnSchedule]
    private DisposeSentinel m_DisposeSentinel;
#endif

  [NativeDisableUnsafePtrRestriction]
  internal ulong* m_Buffer;

  private Allocator m_Allocator;

  /// <summary>
  /// Creates empty NativeBitArraySlim
  /// </summary>
  /// <param name="numBits">
  /// Number of bits in use for compact bool array.
  /// </param>
  /// <param name="allocator">Allocator configuration for internal buffers.</param>
  /// <exception cref="ArgumentException"/>
  /// <param name="options">Allocation option for buffer.</param>
  public NativeBitArraySlim(int numBits, Allocator allocator,
    NativeArrayOptions options = NativeArrayOptions.ClearMemory)
  {
    if (numBits <= 0)
      throw new ArgumentException("length must be greater than 0.");

    ULongCount = (numBits + 63) >> 6;
    Length = numBits;
    m_Allocator = allocator;
    m_Buffer = (ulong*)UnsafeUtility.Malloc(
      size: UnsafeUtility.SizeOf<ulong>() * ULongCount,
      alignment: UnsafeUtility.AlignOf<ulong>(), allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, callSiteStackDepth: 1, allocator);
#endif

    if (options != NativeArrayOptions.ClearMemory)
      return;

    Clear();
  }

  /// <summary>
  /// Container has allocated buffers and is safe to use.
  /// </summary>
  public readonly bool IsCreated => m_Buffer != null;

  /// <summary>
  /// Number of bits in the array.
  /// </summary>
  public int Length { get; }

  /// <summary>
  /// Number of ulongs allocated to contain the bit array.
  /// </summary>
  internal int ULongCount { get; }

  /// <summary>
  /// Get a read-only view of the bit array.
  /// </summary>
  public ReadOnly AsReadOnly()
  {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      return new ReadOnly(m_Buffer, Length, ref m_Safety);
#else
    return new ReadOnly(m_Buffer, Length);
#endif
  }

  /// <summary>
  /// Set the bit at <paramref name="pos"/> to <paramref name="value"/>
  /// </summary>
  /// <param name="pos">index of bit to set</param>
  /// <param name="value">value to set bit to</param>
  public void Set(int pos, bool value)
  {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if ((uint)pos >= (uint)Length)
        throw new IndexOutOfRangeException($"Index {pos} is out of range (must be between 0 and {Length - 1}).");

      AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

    int index = pos >> 6;
    ulong bit = 1UL << pos;
    int intBool = value ? 1 : 0;
    ulong num2 = (ulong)((long)m_Buffer[index] & ~(long)bit | -intBool & (long)bit);
    m_Buffer[index] = num2;
  }

  /// <summary>
  /// Check if bit at <paramref name="pos"/> is set.
  /// </summary>
  /// <param name="pos">
  /// The zero-based position of the bit to check. Must be greater than or equal to 0 and
  /// less than the length of the collection.
  /// </param>
  /// <returns>Value of the bit</returns>
  public readonly bool IsSet(int pos)
  {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if ((uint)pos >= (uint)Length)
        throw new IndexOutOfRangeException($"Index {pos} is out of range (must be between 0 and {Length - 1}).");

      AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif

    int index = pos >> 6;
    ulong num = 1UL << pos;
    return (m_Buffer[index] & num) > 0UL;
  }

  public static bool IsSet(ulong* bits, int pos)
  {
    int index = pos >> 6;
    ulong num = 1UL << pos;
    return (bits[index] & num) > 0UL;
  }

  /// <summary>
  /// Sets all bits in the buffer to 0
  /// </summary>
  public void Clear()
  {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

    UnsafeUtility.MemClear(m_Buffer, (long)UnsafeUtility.SizeOf<ulong>() * ULongCount);
  }

  /// <summary>
  /// Frees memory allocated for this container.
  /// </summary>
  public void Dispose()
  {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if (!UnsafeUtility.IsValidAllocator(m_Allocator))
        throw new InvalidOperationException("Trying to dispose an object with an invalid allocator.");

      DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

    UnsafeUtility.Free(m_Buffer, m_Allocator);
    m_Buffer = null;
    m_Allocator = Allocator.Invalid;
  }

  /// <summary>
  /// Frees memory allocated for this container scheduled to a job.
  /// </summary>
  /// <param name="inputDeps">JobHandle dependency</param>
  /// <returns>JobHandle of the dispose job that was scheduled.</returns>
  public JobHandle Dispose(JobHandle inputDeps)
  {
    if (!IsCreated)
      return default;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
      DisposeSentinel.Clear(ref m_DisposeSentinel);
#endif

    JobHandle jobHandle = new DisposeJob
    {
      data = new DisposeData
      {
        m_Buffer = m_Buffer,
        m_Allocator = m_Allocator
      }
    }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      AtomicSafetyHandle.Release(m_Safety);
#endif
    m_Buffer = null;
    m_Allocator = Allocator.Invalid;
    return jobHandle;
  }

  private struct DisposeData : IDisposable
  {
    [NativeDisableUnsafePtrRestriction]
    public ulong* m_Buffer;
    public Allocator m_Allocator;

    public void Dispose()
    {
      UnsafeUtility.Free(m_Buffer, m_Allocator);
    }
  }

  private struct DisposeJob : IJob
  {
    public DisposeData data;

    public void Execute()
    {
      data.Dispose();
    }
  }

  [NativeContainer, PublicAPI]
  [NativeContainerIsReadOnly]
  [DebuggerDisplay("Length = {Length}")]
  public readonly struct ReadOnly
  {
    [NativeDisableUnsafePtrRestriction]
    private readonly ulong* m_Buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      private readonly AtomicSafetyHandle m_Safety;

      internal ReadOnly(ulong* buffer, int length, ref AtomicSafetyHandle safety)
      {
        m_Buffer = buffer;
        Length = length;
        m_Safety = safety;
      }
#else
    internal ReadOnly(ulong* buffer, int length)
    {
      m_Buffer = buffer;
      Length = length;
    }
#endif

    public int Length { get; }

    public bool IsCreated => m_Buffer != null;

    public bool IsSet(int pos)
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        if ((uint)pos >= (uint)Length)
          throw new IndexOutOfRangeException($"Index {pos} is out of range (must be between 0 and {Length - 1}).");

        AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
      return NativeBitArraySlim.IsSet(m_Buffer, pos);
    }

    public ulong* GetUnsafePtr()
    {
      return m_Buffer;
    }

    public ulong* GetUnsafeReadOnlyPtr()
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
      return m_Buffer;
    }
  }
}
