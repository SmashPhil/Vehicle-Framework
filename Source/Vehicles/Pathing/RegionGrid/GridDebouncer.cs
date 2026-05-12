using System;
using System.Collections.Generic;
using Unity.Collections;
using Verse;

namespace Vehicles;

public class GridDebouncer : IDisposable
{
	private readonly Map map;
	private readonly List<IGridDebouncerSource> sources;

  private NativeBitArray dirtyCells;

  public GridDebouncer(Map map, List<IGridDebouncerSource> sources)
	{
		this.map = map;
		this.sources = sources;
		dirtyCells = new NativeBitArray(map.cellIndices.NumGridCells, Allocator.Persistent);

    foreach (IGridDebouncerSource source in sources)
    {
      source.ActiveDebouncer = this;
    }
	}

	public void SetDirty(IntVec3 cell)
	{
		SetDirty(map.cellIndices.CellToIndex(cell));
	}

	public void SetDirty(int index)
	{
		dirtyCells.Set(index, true);
	}

	public void ExecuteAll()
	{
		for (int i = 0; i < dirtyCells.Length; i++)
		{
			if (dirtyCells.IsSet(i))
			{
				foreach (IGridDebouncerSource source in sources)
				{
					source.Execute(i);
				}
			}
		}
	}

	public void Dispose()
	{
		dirtyCells.Dispose();

    foreach (IGridDebouncerSource source in sources)
    {
      source.ActiveDebouncer = null;
    }
  }
}