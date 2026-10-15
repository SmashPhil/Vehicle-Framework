using System;
using System.Diagnostics;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

// NOTE - Using Hungarian notation since Unity expects certain fields to be spelled exactly in that manner
// otherwise it will spit out errors for NativeContainer.
// ReSharper disable InconsistentNaming

namespace CoreLib.Collections;

/// <summary>
/// MinHeap implementation for A-star pathfinding. Elements pushed to heap must directly map to cell indices.
/// </summary>
/// <remarks>
/// <a href="https://en.wikipedia.org/wiki/Min-max_heap"/>
/// </remarks>
[NativeContainer, PublicAPI]
[DebuggerDisplay("Length = {Length}")]
public unsafe struct NativeMinHeap : IDisposable
{
  // Uses a 4-nary tree
  public const int LEAF_ARITY = 4;
  // Tail padding so ShiftDown can do a 4-wide cost load even when a node has fewer than 4 children.
  public const int PADDING = LEAF_ARITY - 1;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
    private AtomicSafetyHandle m_Safety;
    [NativeSetClassTypeToNullOnSchedule]
    private DisposeSentinel m_DisposeSentinel;
#endif

  [NativeDisableUnsafePtrRestriction]
  private int* m_Buffer;
  [NativeDisableUnsafePtrRestriction]
  private int* m_CostBuffer;
  [NativeDisableUnsafePtrRestriction]
  private int* m_IndexBuffer;
  [NativeDisableUnsafePtrRestriction]
  private Header* m_Header;

  private readonly Allocator m_Allocator;

  private struct Header
  {
    public int m_Capacity;
    public int m_Allocated;
    public int m_Length;
  }

  /// <summary>
  /// Creates empty NativeMinHeap and allocates 2 internal buffers using <paramref name="allocator"/> setting.
  /// </summary>
  /// <param name="capacity">
  /// Capacity of internal buffers.
  /// </param>
  /// <param name="allocator">Allocator configuration for internal buffers.</param>
  /// <exception cref="ArgumentException"/>
  public NativeMinHeap(int capacity, Allocator allocator)
  {
    if (capacity <= 0)
      throw new ArgumentException("capacity must be greater than 0.");

    int allocated = capacity + PADDING;
    m_Allocator = allocator;

    m_Buffer = (int*)UnsafeUtility.Malloc(
      size: (long)sizeof(int) * allocated,
      alignment: UnsafeUtility.AlignOf<int>(), allocator);
    m_CostBuffer = (int*)UnsafeUtility.Malloc(
      size: (long)sizeof(int) * allocated,
      alignment: UnsafeUtility.AlignOf<int>(), allocator);
    m_IndexBuffer = (int*)UnsafeUtility.Malloc(
      size: (long)sizeof(int) * allocated,
      alignment: UnsafeUtility.AlignOf<int>(), allocator);
    m_Header = (Header*)UnsafeUtility.Malloc(
      size: sizeof(Header),
      alignment: UnsafeUtility.AlignOf<Header>(), allocator);

    m_Header->m_Capacity = capacity;
    m_Header->m_Length = 0;
    m_Header->m_Allocated = allocated;
    for (int i = 0; i < allocated; i++)
    {
      m_CostBuffer[i] = int.MaxValue;
      m_IndexBuffer[i] = -1;
    }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, callSiteStackDepth: 1, allocator);
#endif
  }

  /// <summary>
  /// Container has allocated buffers and is safe to use.
  /// </summary>
  public readonly bool IsCreated => m_Header != null;

  /// <summary>
  /// Number of nodes in the min heap.
  /// </summary>
  public readonly int Length => m_Header->m_Length;

  /// <summary>
  /// Total number of nodes allowed in the min heap.
  /// </summary>
  public readonly int Capacity => m_Header->m_Capacity;

  /// <summary>
  /// Internal size of buffer, including padding for SIMD loads.
  /// </summary>
  internal readonly int Allocated => m_Header->m_Allocated;

  /// <summary>
  /// Read index from buffer with no bounds check.
  /// </summary>
  /// <remarks>NOTE: For internal testing only.</remarks>
  internal readonly int PeekCostUnsafe(int index)
  {
    return m_CostBuffer[index];
  }

  /// <summary>
  /// Look at the next node in the min heap without removing it.
  /// </summary>
  /// <returns>Element at the top of the heap.</returns>
  /// <exception cref="InvalidOperationException"/>
  public readonly int Peek()
  {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if (m_Header->m_Length == 0)
        throw new InvalidOperationException("Peeking with no elements in the heap.");
#endif

    return m_Buffer[0];
  }

  [WriteAccessRequired]
  public void Push(int element, int cost)
  {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if ((uint)element >= m_Header->m_Capacity)
        throw new IndexOutOfRangeException("Element out of range of buffer.");

      AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

    int heapIndex = m_IndexBuffer[element];
    // Update in place
    if (heapIndex >= 0)
    {
      if (cost >= m_CostBuffer[heapIndex])
        return;

      ShiftUp(heapIndex, element, cost);
      return;
    }

    // Insert
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if (m_Header->m_Length == m_Header->m_Capacity)
        throw new IndexOutOfRangeException("Max capacity reached.");
#endif

    ShiftUp(m_Header->m_Length, element, cost);
    m_Header->m_Length++;
  }

  [WriteAccessRequired]
  public int Pop()
  {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if (m_Header->m_Length == 0)
        throw new InvalidOperationException("Popping with no elements in the heap.");

      AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

    int element = m_Buffer[0];
    m_IndexBuffer[element] = -1;
    m_Header->m_Length--;
    if (m_Header->m_Length > 0)
    {
      int last = m_Buffer[m_Header->m_Length];
      int lastCost = m_CostBuffer[m_Header->m_Length];
      ShiftDown(0, last, lastCost);
    }
    m_CostBuffer[m_Header->m_Length] = int.MaxValue;
    return element;
  }

  private void ShiftUp(int index, int element, int cost)
  {
    while (index > 0)
    {
      int parentIndex = (index - 1) / LEAF_ARITY;
      int parentCost = m_CostBuffer[parentIndex];

      if (cost >= parentCost)
        break;

      int parent = m_Buffer[parentIndex];
      m_Buffer[index] = parent;
      m_CostBuffer[index] = parentCost;
      m_IndexBuffer[parent] = index;
      index = parentIndex;
    }
    m_Buffer[index] = element;
    m_CostBuffer[index] = cost;
    m_IndexBuffer[element] = index;
  }

  private void ShiftDown(int index, int element, int cost)
  {
    int firstChild;
    while ((firstChild = index * LEAF_ARITY + 1) < m_Header->m_Length)
    {
      int4 childCosts = *(int4*)(m_CostBuffer + firstChild);
      int minChildCost = math.cmin(childCosts);

      if (cost <= minChildCost)
        break;

      int mask = math.bitmask(childCosts == minChildCost);
      int smallestChildIndex = firstChild + math.tzcnt(mask);

      int moved = m_Buffer[smallestChildIndex];
      m_Buffer[index] = moved;
      m_CostBuffer[index] = minChildCost;
      m_IndexBuffer[moved] = index;
      index = smallestChildIndex;
    }
    m_Buffer[index] = element;
    m_CostBuffer[index] = cost;
    m_IndexBuffer[element] = index;
  }

  public void Clear()
  {
    for (int i = 0; i < m_Header->m_Length; i++)
    {
      m_CostBuffer[i] = int.MaxValue;
      m_IndexBuffer[m_Buffer[i]] = -1;
    }
    m_Header->m_Length = 0;
  }

  public void Dispose()
  {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
      if (!UnsafeUtility.IsValidAllocator(m_Allocator))
        throw new InvalidOperationException("Trying to dispose an object with an invalid allocator.");

      DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif

    Clear();

    UnsafeUtility.Free(m_Buffer, m_Allocator);
    m_Buffer = null;
    UnsafeUtility.Free(m_CostBuffer, m_Allocator);
    m_CostBuffer = null;
    UnsafeUtility.Free(m_IndexBuffer, m_Allocator);
    m_IndexBuffer = null;
    UnsafeUtility.Free(m_Header, m_Allocator);
    m_Header = null;
  }
}
