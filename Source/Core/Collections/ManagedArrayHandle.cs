
using System;
using System.Diagnostics;
using CoreLib.Performance;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

// NOTE - Using Hungarian notation since Unity expects certain fields to be spelled exactly in that manner
// otherwise it will spit out errors for NativeContainer.
// ReSharper disable InconsistentNaming

namespace CoreLib.Collections;

[PublicAPI]
[DebuggerDisplay("Length = {Length}")]
public unsafe struct ManagedArrayHandle<T> : INativeDisposable where T : unmanaged
{
  [NativeDisableUnsafePtrRestriction]
  private T* m_Buffer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    private AtomicSafetyHandle m_Safety;
    [NativeSetClassTypeToNullOnSchedule]
    private DisposeSentinel m_DisposeSentinel;
#endif

  private int m_Length;
  private ulong m_GCHandle;
  private Allocator m_Allocator;

  public ManagedArrayHandle(T[] array, Allocator allocator)
  {
    m_Allocator = allocator;
    m_Length = array.Length;

    m_Buffer = (T*)UnsafeUtility.PinGCArrayAndGetDataAddress(array, out ulong gcHandle);
    m_GCHandle = gcHandle;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, callSiteStackDepth: 1, allocator);
#endif
  }

  public bool IsCreated => m_Buffer != null;

  public int Length => IsCreated ? m_Length : 0;

  public T this[int index]
  {
    get
    {
      if (!IsCreated)
        throw new ObjectDisposedException(nameof(ManagedArrayHandle<T>));

      return m_Buffer[index];
    }
  }

  public void Dispose()
  {
    if (!IsCreated)
      return;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if (UnityThread.IsInMainThread)
      {
        DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
      }
#endif

    UnsafeUtility.ReleaseGCObject(m_GCHandle);
    m_GCHandle = 0;
    m_Buffer = null;
  }

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
      data = new DisposeData { m_GCHandle = m_GCHandle }
    }.Schedule(inputDeps);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      AtomicSafetyHandle.Release(m_Safety);
#endif
    m_GCHandle = 0;
    m_Buffer = null;
    m_Allocator = Allocator.Invalid;
    m_Length = 0;
    return jobHandle;
  }

  private struct DisposeData : IDisposable
  {
    public ulong m_GCHandle;

    public void Dispose()
    {
      UnsafeUtility.ReleaseGCObject(m_GCHandle);
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
}
