using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public class ChunkSet
	{
		private HashSet<IntVec3> cells;

		public HashSet<IntVec3> Cells => cells;

		public ChunkSet(List<VehicleRegion> regions)
		{
			CacheCells(regions);
		}

		public ChunkSet(HashSet<VehicleRegion> regions)
		{
			CacheCells(regions);
		}

		public bool NullOrEmpty()
		{
			return cells is null || cells.Count == 0;
		}

		private void CacheCells(IEnumerable<VehicleRegion> regions)
		{
			cells = new HashSet<IntVec3>();
			foreach (VehicleRegion region in regions)
			{
				cells.AddRange(region.Cells);
			}
		}
	}
}
