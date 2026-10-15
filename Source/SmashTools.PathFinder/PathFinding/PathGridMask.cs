using System;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  public abstract class PathGridMask : IDisposable
  {
    protected internal NativeArray<int> array;

    private readonly IPathGridSync sync;

    protected PathGridMask(int2 mapSize, [CanBeNull] IPathGridSync sync = null)
    {
      Length = mapSize.x * mapSize.y;
      array = new NativeArray<int>(Length, Allocator.Persistent);
      this.sync = sync;
    }

    public int Length { get; }

    public NativeArray<int>.ReadOnly ArrayUnsafe => array.AsReadOnly();

    public int this[int index]
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