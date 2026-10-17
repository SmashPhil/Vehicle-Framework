using System;
using JetBrains.Annotations;
using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  public abstract class HeuristicGrid : IDisposable
  {
    protected internal NativeArray<HeuristicWeight> array;

    private readonly IPathGridSync sync;

    protected HeuristicGrid(int2 mapSize, [CanBeNull] IPathGridSync sync = null)
    {
      Length = mapSize.x * mapSize.y;
      array = new NativeArray<HeuristicWeight>(Length, Allocator.Persistent);
      this.sync = sync;
    }

    public int Length { get; }

    public NativeArray<HeuristicWeight>.ReadOnly ArrayUnsafe => array.AsReadOnly();

    public HeuristicWeight this[int index]
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

    public struct Data
    {
      internal readonly HeuristicGrid grid;
      internal readonly Heuristic weights;

      public Data(HeuristicGrid grid)
      {
        this.grid = grid;
        weights = Heuristic.Default;
      }

      public Data(HeuristicGrid grid, Heuristic weights)
      {
        this.grid = grid;
        this.weights = weights;
      }
    }
  }
}