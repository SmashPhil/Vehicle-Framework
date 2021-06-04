using System.Collections.Generic;
using Verse;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public sealed class VehicleRegionGrid
	{
		private const int CleanSquaresPerFrame = 16;

		public static HashSet<VehicleRegion> allRegionsYielded = new HashSet<VehicleRegion>();

		private readonly Map map;

		private VehicleRegion[] regionGrid;
		private int curCleanIndex;

		public List<VehicleRoom> allRooms = new List<VehicleRoom>();
		public HashSet<VehicleRegion> drawnRegions = new HashSet<VehicleRegion>();

		public VehicleRegionGrid(Map map)
		{
			this.map = map;
			regionGrid = new VehicleRegion[map.cellIndices.NumGridCells];
		}

		public VehicleRegion[] DirectGrid
		{
			get
			{
				return regionGrid;
			}
		}

		public IEnumerable<VehicleRegion> AllRegions_NoRebuild_InvalidAllowed
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

		public IEnumerable<VehicleRegion> AllRegions
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

		public VehicleRegion GetValidRegionAt(IntVec3 c)
		{
			if(!c.InBoundsShip(map))
			{
				Log.Error("Tried to get valid water region out of bounds at " + c);
			}
			if(!map.GetCachedMapComponent<VehicleMapping>().VehicleRegionAndRoomUpdater.Enabled && map.GetCachedMapComponent<VehicleMapping>().VehicleRegionAndRoomUpdater.AnythingToRebuild)
			{
				Log.Warning("Trying to get valid water region at " + c + " but RegionAndRoomUpdater is disabled. The result may be incorrect.");
			}
			map.GetCachedMapComponent<VehicleMapping>().VehicleRegionAndRoomUpdater.TryRebuildWaterRegions();
			VehicleRegion region = regionGrid[map.cellIndices.CellToIndex(c)];
			
			return !(region is null) && region.valid ? region : null;
		}

		public VehicleRegion GetValidRegionAt_NoRebuild(IntVec3 c)
		{
			if(!c.InBoundsShip(map))
			{
				Log.Error("Tried to get valid region out of bounds at " + c);
			}
			VehicleRegion region = regionGrid[map.cellIndices.CellToIndex(c)];
			return !(region is null) && region.valid ? region : null;
		}

		public VehicleRegion GetRegionAt_NoRebuild_InvalidAllowed(IntVec3 c)
		{
			return regionGrid[map.cellIndices.CellToIndex(c)];
		}

		public void SetRegionAt(IntVec3 c, VehicleRegion reg)
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
				VehicleRegion region = regionGrid[curCleanIndex];
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
					VehicleRegion validRegionAt = GetValidRegionAt(c);
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
				VehicleRegion regionAt_NoRebuild_InvalidAllowed = GetRegionAt_NoRebuild_InvalidAllowed(intVec);
				if (!(regionAt_NoRebuild_InvalidAllowed is null))
				{
					regionAt_NoRebuild_InvalidAllowed.DebugDrawMouseover();
				}
			}
		}
	}
}
