using SmashTools;
using SmashTools.Performance;
using System.Collections.Generic;
using System.Threading;
using Verse;

namespace Vehicles
{
	/// <summary>
	/// Region grid for vehicle specific regions
	/// </summary>
	public sealed class VehicleRegionGrid : VehicleRegionManager
    {
		private const int CleanSquaresPerFrame = 16;

		//Thread Safe - Only accessed from the same thread within the same method
		private readonly HashSet<VehicleRegion> allRegionsYielded = new HashSet<VehicleRegion>();
		
		//Thread safe - Only used inside UpdateClean
		private int curCleanIndex;
		private readonly VehicleRegion[] regionGrid;

		public readonly ConcurrentSet<VehicleRoom> allRooms = new ConcurrentSet<VehicleRoom>();
		
		public VehicleRegionGrid(VehicleMapping mapping, VehicleDef createdFor) : base(mapping, createdFor)
		{
			regionGrid = new VehicleRegion[mapping.map.cellIndices.NumGridCells];
		}

		/// <summary>
		/// Region grid getter
		/// </summary>
		public VehicleRegion[] DirectGrid
		{
			get
			{
				return regionGrid;
			}
		}

		/// <summary>
		/// Yield all non-null regions
		/// </summary>
		public IEnumerable<VehicleRegion> AllRegions_NoRebuild_InvalidAllowed
		{
			get
			{
				allRegionsYielded.Clear();
				try
				{
					int count = mapping.map.cellIndices.NumGridCells;
					for (int i = 0; i < count; i++)
					{
						VehicleRegion region = GetRegionAt(i);
						if (region != null && !allRegionsYielded.Contains(region))
						{
							yield return region;
							allRegionsYielded.Add(region);
						}
					}
				}
				finally
				{
					allRegionsYielded.Clear();
				}
			}
		}

		/// <summary>
		/// Yield all valid regions
		/// </summary>
		public IEnumerable<VehicleRegion> AllRegions
		{
			get
			{
				allRegionsYielded.Clear();
				try
				{
					int count = mapping.map.cellIndices.NumGridCells;
					for (int i = 0; i < count; i++)
					{
						VehicleRegion region = GetRegionAt(i);
						if (region != null && region.valid && !allRegionsYielded.Contains(region))
						{
							yield return region;
							allRegionsYielded.Add(region);
						}
					}
				}
				finally
				{
					allRegionsYielded.Clear();
				}
			}
		}

		/// <summary>
		/// Retrieve valid region at <paramref name="cell"/>
		/// </summary>
		/// <param name="cell"></param>
		public VehicleRegion GetValidRegionAt(IntVec3 cell)
		{
			if (!cell.InBounds(mapping.map))
			{
				Log.Error($"Tried to get valid vehicle region for {createdFor} out of bounds at {cell}");
				return null;
			}
			if (!mapping[createdFor].VehicleRegionAndRoomUpdater.Enabled &&
				mapping[createdFor].VehicleRegionAndRoomUpdater.AnythingToRebuild)
			{
				Log.Warning($"Trying to get valid vehicle region for {createdFor} at {cell} but RegionAndRoomUpdater is disabled. The result may be incorrect.");
			}
			mapping[createdFor].VehicleRegionAndRoomUpdater.TryRebuildVehicleRegions();
			VehicleRegion region = GetRegionAt(cell);
			return (region != null && region.valid) ? region : null;
		}

		/// <summary>
		/// Get valid region at <paramref name="cell"/> without rebuilding the region grid
		/// </summary>
		/// <param name="c"></param>
		public VehicleRegion GetValidRegionAt_NoRebuild(IntVec3 cell)
		{
			if (mapping.map is null)
			{
				Log.Error($"Tried to get valid region with null map.");
				return null;
			}
			if (mapping.map.info is null)
			{
				Log.Error($"Tried to get map info with null info. Map = {mapping.map.uniqueID}");
				return null;
			}
			if (regionGrid is null)
			{
				Log.Error($"Tried to get valid region with null regionGrid. Have the vehicle regions been instantiated yet?");
				return null;
			}
			if (!cell.InBounds(mapping.map))
			{
				Log.Error("Tried to get valid region out of bounds at " + cell);
				return null;
			}
			VehicleRegion region = GetRegionAt(cell);
			return region != null && region.valid ? region : null;
		}

		/// <summary>
		/// Get any existing region at <paramref name="cell"/>
		/// </summary>
		/// <param name="cell"></param>
		public VehicleRegion GetRegionAt(IntVec3 cell)
		{
			int index = mapping.map.cellIndices.CellToIndex(cell);
			return GetRegionAt(index);
		}

		/// <summary>
		/// Get any existing region at <paramref name="cell"/>
		/// </summary>
		/// <param name="cell"></param>
		public VehicleRegion GetRegionAt(int index)
		{
			return regionGrid[index];
		}

		/// <summary>
		/// Set existing region at <paramref name="cell"/> to <paramref name="region"/>
		/// </summary>
		/// <param name="c"></param>
		/// <param name="reg"></param>
		public void SetRegionAt(IntVec3 cell, VehicleRegion region)
		{
			int index = mapping.map.cellIndices.CellToIndex(cell);
			Interlocked.CompareExchange(ref regionGrid[index], region, regionGrid[index]);
		}

		public void SetRegionAt(int index, VehicleRegion region)
		{
			VehicleRegion other = regionGrid[index];
			other?.DecrementRefCount();
			region?.IncrementRefCount();
			Interlocked.CompareExchange(ref regionGrid[index], region, regionGrid[index]);
		}

		/// <summary>
		/// Update regionGrid and purge all invalid regions
		/// </summary>
		public void UpdateClean()
		{
			for (int i = 0; i < CleanSquaresPerFrame; i++)
			{
				if (curCleanIndex >= regionGrid.Length)
				{
					curCleanIndex = 0;
				}
				VehicleRegion region = regionGrid[curCleanIndex];
				if (region != null && !region.valid)
				{
					VehicleRegionMaker.PushToBuffer(region);
					SetRegionAt(curCleanIndex, null);
				}
				curCleanIndex++;
			}
		}

		/// <summary>
		/// Draw debug data
		/// </summary>
		public void DebugDraw(DebugRegionType debugRegionType)
		{
			if (mapping.map != Find.CurrentMap)
			{
				return;
			}
			if (DebugProperties.debug)
			{
				foreach (VehicleRegion debugRegion in AllRegions_NoRebuild_InvalidAllowed)
				{
					debugRegion.DebugDrawMouseover(debugRegionType);
				}
			}
			IntVec3 intVec = UI.MouseCell();
			if (intVec.InBounds(mapping.map))
			{
				VehicleRegion regionAt_NoRebuild_InvalidAllowed = GetRegionAt(intVec);
				regionAt_NoRebuild_InvalidAllowed?.DebugDrawMouseover(debugRegionType);
			}
		}

		/// <summary>
		/// Draw OnGUI label path costs
		/// </summary>
		public void DebugOnGUI(DebugRegionType debugRegionType)
		{
			IntVec3 intVec = UI.MouseCell();
			if (intVec.InBounds(mapping.map))
			{
				VehicleRegion regionAt_NoRebuild_InvalidAllowed = GetRegionAt(intVec);
				regionAt_NoRebuild_InvalidAllowed?.DebugOnGUIMouseover(debugRegionType);
			}
		}
	}
}
