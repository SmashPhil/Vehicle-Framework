
using System;
using System.Diagnostics;
using JetBrains.Annotations;
using CoreLib.Collections;
using Unity.Collections;
using Unity.Mathematics;

namespace SmashTools.PathFinder
{
  [PublicAPI]
  public struct BitOffset
  {
    public PathBoolGrid boolGrid;
    public PathBoolGrid.Cost cost;
  }

  /// <summary>
  /// Bool grid for applying variable cost offsets based on the true / false state of a cell.
  /// </summary>
  [PublicAPI]
  [DebuggerDisplay("Length = {Length}")]
  public abstract class PathBoolGrid : IDisposable
  {
    protected internal NativeBitArraySlim array;

    private readonly IPathGridSync sync;

    /// <summary>
    /// Offset to apply if bit is set or unset at position
    /// </summary>
    [PublicAPI]
    public struct Cost
    {
      public short set;
      public short unset;
    }

    protected PathBoolGrid(int2 mapSize, [CanBeNull] IPathGridSync sync = null)
    {
      Length = mapSize.x * mapSize.y;
      array = new NativeBitArraySlim(Length, Allocator.Persistent);
      this.sync = sync;
    }

    public int Length { get; }

    public NativeBitArraySlim.ReadOnly ArrayUnsafe => array.AsReadOnly();

    public bool this[int index]
    {
      get
      {
        if (index < 0 || index >= array.Length)
          throw new IndexOutOfRangeException(index.ToString());

        return array.IsSet(index);
      }
      set
      {
        if (index < 0 || index >= array.Length)
          throw new IndexOutOfRangeException(index.ToString());

        sync?.NotifyWritingToGrid();
        array.Set(index, value);
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