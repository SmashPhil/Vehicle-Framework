using System.Collections.Generic;
using Verse;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public sealed class WaterRegionGrid
	{
		private const int CleanSquaresPerFrame = 16;

		public static HashSet<WaterRegion> allRegionsYielded = new HashSet<WaterRegion>();

		private readonly Map map;

		private WaterRegion[] regionGrid;
		private int curCleanIndex;

		public List<WaterRoom> allRooms = new List<WaterRoom>();
		public HashSet<WaterRegion> drawnRegions = new HashSet<WaterRegion>();

		public WaterRegionGrid(Map map)
		{
			this.map = map;
			regionGrid = new WaterRegion[map.cellIndices.NumGridCells];
		}

		public WaterRegion[] DirectGrid
		{
			get
			{
				return regionGrid;
			}
		}

		public IEnumerable<WaterRegion> AllRegions_NoRebuild_InvalidAllowed
		{
			get
			{
				allRegionsYielded.Clear();
				try
				{
					int count = map.cellIndices.NumGridCells;
					for(int i = 0; i < count; i++)
					{
						if(!(regionGrid[i] is null) && !allRegionsYielded.Contains(regionGrid[i]))
						{
							yield return regionGrid[i];
							allRegionsYielded.Add(regionGrid[i]);
						}
					}
				}
				finally
				{
					allRegionsYielded.Clear();
				}
				yield break;
			}
		}

		public IEnumerable<WaterRegion> AllRegions
		{
			get
			{
				allRegionsYielded.Clear();
				try
				{
					int count = map.cellIndices.NumGridCells;
					for(int i = 0; i < count; i++)
					{
						if(!(regionGrid[i] is null) && regionGrid[i].valid && !allRegionsYielded.Contains
							(regionGrid[i]))
						{
							yield return regionGrid[i];
							allRegionsYielded.Add(regionGrid[i]);
						}
					}
				}
				finally
				{
					allRegionsYielded.Clear();
				}
				yield break;
			}
		}

		public WaterRegion GetValidRegionAt(IntVec3 c)
		{
			if(!c.InBoundsShip(map))
			{
				Log.Error("Tried to get valid water region out of bounds at " + c);
			}
			if(!map.GetCachedMapComponent<WaterMap>().WaterRegionAndRoomUpdater.Enabled && map.GetCachedMapComponent<WaterMap>().WaterRegionAndRoomUpdater.AnythingToRebuild)
			{
				Log.Warning("Trying to get valid water region at " + c + " but RegionAndRoomUpdater is disabled. The result may be incorrect.");
			}
			map.GetCachedMapComponent<WaterMap>().WaterRegionAndRoomUpdater.TryRebuildWaterRegions();
			WaterRegion region = regionGrid[map.cellIndices.CellToIndex(c)];
			
			return !(region is null) && region.valid ? region : null;
		}

		public WaterRegion GetValidRegionAt_NoRebuild(IntVec3 c)
		{
			if(!c.InBoundsShip(map))
			{
				Log.Error("Tried to get valid region out of bounds at " + c);
			}
			WaterRegion region = regionGrid[map.cellIndices.CellToIndex(c)];
			return !(region is null) && region.valid ? region : null;
		}

		public WaterRegion GetRegionAt_NoRebuild_InvalidAllowed(IntVec3 c)
		{
			return regionGrid[map.cellIndices.CellToIndex(c)];
		}

		public void SetRegionAt(IntVec3 c, WaterRegion reg)
		{
			regionGrid[map.cellIndices.CellToIndex(c)] = reg;
		}

		public void UpdateClean()
		{
			for(int i = 0; i < CleanSquaresPerFrame; i++)
			{
				if(curCleanIndex >= regionGrid.Length)
				{
					curCleanIndex = 0;
				}
				WaterRegion region = regionGrid[curCleanIndex];
				if(!(region is null) && !region.valid)
				{
					regionGrid[curCleanIndex] = null;
				}
				curCleanIndex++;
			}
		}

		public void DebugDraw()
		{
			if(map != Find.CurrentMap)
			{
				return;
			}
			
			//Region Traversal
			if(VehicleHarmony.debug)
			{
				CellRect currentViewRect = Find.CameraDriver.CurrentViewRect;
				currentViewRect.ClipInsideMap(map);
				foreach(IntVec3 c in currentViewRect)
				{
					WaterRegion validRegionAt = GetValidRegionAt(c);
					if(!(validRegionAt is null) && !drawnRegions.Contains(validRegionAt))
					{
						validRegionAt.DebugDraw();
						drawnRegions.Add(validRegionAt);
					}
				}
				drawnRegions.Clear();
			}
			IntVec3 intVec = Verse.UI.MouseCell();
			if(intVec.InBoundsShip(map))
			{
				//Room?
				//Room Group?
				WaterRegion regionAt_NoRebuild_InvalidAllowed = GetRegionAt_NoRebuild_InvalidAllowed(intVec);
				if (!(regionAt_NoRebuild_InvalidAllowed is null))
				{
					regionAt_NoRebuild_InvalidAllowed.DebugDrawMouseover();
				}
			}
		}
	}
}
