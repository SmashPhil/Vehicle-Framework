using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Collections;
using Verse;

namespace Vehicles;

public class GridDebouncer : IDisposable
{
	private readonly Map map;
	private readonly NativeBitArray dirtyCells;
	private readonly List<IGridDebouncerSource> sources;

	public GridDebouncer(Map map, List<IGridDebouncerSource> sources)
	{
		this.map = map;
		this.sources = sources;
		dirtyCells = new NativeBitArray(map.cellIndices.NumGridCells, Allocator.Persistent);
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
	}
}