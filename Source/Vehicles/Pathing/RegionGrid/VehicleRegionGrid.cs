using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Region grid for vehicle specific regions
	/// </summary>
	public sealed class VehicleRegionGrid
	{
		public const int CleanSquaresPerFrame = 16;

		public static readonly HashSet<VehicleRegion> allRegionsYielded = new HashSet<VehicleRegion>();
		public static int vehicleRegionGridIndexChecking = 0;

		private readonly VehicleMapping mapping;
		private readonly VehicleDef createdFor;

		private int curCleanIndex;

		private VehicleRegion[] regionGrid;

		public readonly List<VehicleRoom> allRooms = new List<VehicleRoom>();

		public VehicleRegionGrid(VehicleMapping mapping, VehicleDef createdFor)
		{
			this.mapping = mapping;
			this.createdFor = createdFor;
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
						if (regionGrid[i] != null && !allRegionsYielded.Contains(regionGrid[i]))
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
						if (regionGrid[i] != null && regionGrid[i].valid && !allRegionsYielded.Contains(regionGrid[i]))
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
			VehicleRegion region = regionGrid[mapping.map.cellIndices.CellToIndex(cell)];

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
			VehicleRegion region = regionGrid[mapping.map.cellIndices.CellToIndex(cell)];
			return region != null && region.valid ? region : null;
		}

		/// <summary>
		/// Get any existing region at <paramref name="cell"/>
		/// </summary>
		/// <param name="cell"></param>
		public VehicleRegion GetRegionAt_NoRebuild_InvalidAllowed(IntVec3 cell)
		{
			return regionGrid[mapping.map.cellIndices.CellToIndex(cell)];
		}

		/// <summary>
		/// Set existing region at <paramref name="cell"/> to <paramref name="region"/>
		/// </summary>
		/// <param name="c"></param>
		/// <param name="reg"></param>
		public void SetRegionAt(IntVec3 cell, VehicleRegion region)
		{
			regionGrid[mapping.map.cellIndices.CellToIndex(cell)] = region;
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
					regionGrid[curCleanIndex] = null;
				}
				curCleanIndex++;
			}
			vehicleRegionGridIndexChecking++;
			if (vehicleRegionGridIndexChecking >= mapping.TotalOwners)
			{
				vehicleRegionGridIndexChecking = 0;
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
			if (VehicleHarmony.debug)
			{
				foreach (VehicleRegion debugRegion in AllRegions_NoRebuild_InvalidAllowed)
				{
					debugRegion.DebugDraw();
				}
			}
			IntVec3 intVec = UI.MouseCell();
			if (intVec.InBounds(mapping.map))
			{
				VehicleRegion regionAt_NoRebuild_InvalidAllowed = GetRegionAt_NoRebuild_InvalidAllowed(intVec);
				if (regionAt_NoRebuild_InvalidAllowed != null)
				{
					regionAt_NoRebuild_InvalidAllowed.DebugDrawMouseover(debugRegionType);
				}
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
				VehicleRegion regionAt_NoRebuild_InvalidAllowed = GetRegionAt_NoRebuild_InvalidAllowed(intVec);
				if (regionAt_NoRebuild_InvalidAllowed != null)
				{
					regionAt_NoRebuild_InvalidAllowed.DebugOnGUIMouseover(debugRegionType);
				}
			}
		}
	}
}
