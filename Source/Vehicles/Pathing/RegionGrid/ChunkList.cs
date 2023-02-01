using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public class ChunkList
	{
		private List<VehicleRegion> regions;
		private HashSet<IntVec3> cells;

		public HashSet<IntVec3> Cells => cells;

		public List<VehicleRegion> Regions => regions;
		
		public ChunkList(List<VehicleRegion> regions)
		{
			this.regions = regions;
			CacheCells();
		}

		public bool NullOrEmpty()
		{
			return regions.NullOrEmpty();
		}

		private void CacheCells()
		{
			foreach (VehicleRegion region in regions)
			{
				cells.AddRange(region.Cells);
			}
		}
	}
}
