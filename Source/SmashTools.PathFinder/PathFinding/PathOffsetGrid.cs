using System;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  /// <summary>
  /// Path grid offsets that can be conditionally applied for a path request.
  /// </summary>
  [PublicAPI]
  public abstract class PathOffsetGrid : IDisposable
  {
    protected internal NativeArray<short> array;

    private readonly IPathGridSync sync;

    protected PathOffsetGrid(int2 mapSize, [CanBeNull] IPathGridSync sync = null)
    {
      Length = mapSize.x * mapSize.y;
      array = new NativeArray<short>(Length, Allocator.Persistent);
      this.sync = sync;
    }

    public int Length { get; }

    public NativeArray<short>.ReadOnly ArrayUnsafe => array.AsReadOnly();

    public short this[int index]
    {
      get
      {
        if (index < 0 || index >= array.Length)
          throw new IndexOutOfRangeException(index.ToString());

        return array[index];
      }
      set
      {
        if (index < 0 || index >= array.Length)
          throw new IndexOutOfRangeException(index.ToString());

        sync?.NotifyWritingToGrid();
        array[index] = value;
      }
    }

    public virtual void Dispose()
    {
      if (!array.IsCreated)
        return;

      array.Dispose();
    }
  }
}