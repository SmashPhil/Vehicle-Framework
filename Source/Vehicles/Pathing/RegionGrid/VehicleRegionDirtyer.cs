using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Region dirtyer handler for recaching
	/// </summary>
	public class VehicleRegionDirtyer
	{
		private readonly Map map;
		private readonly VehicleDef vehicleDef;

		private readonly HashSet<IntVec3> dirtyCells = new HashSet<IntVec3>();

		private readonly List<VehicleRegion> regionsToDirty = new List<VehicleRegion>();

		public VehicleRegionDirtyer(Map map, VehicleDef vehicleDef)
		{
			this.map = map;
			this.vehicleDef = vehicleDef;
		}

		/// <summary>
		/// <see cref="dirtyCells"/> getter
		/// </summary>
		public HashSet<IntVec3> DirtyCells => dirtyCells;

		/// <summary>
		/// Any dirty cells registered
		/// </summary>
		public bool AnyDirty
		{
			get
			{
				return dirtyCells.Count > 0;
			}
		}

		/// <summary>
		/// Clear all dirtyed cells
		/// </summary>
		internal void SetAllClean()
		{
			dirtyCells.Clear();
		}

		/// <summary>
		/// Set all cells and regions to dirty status
		/// </summary>
		internal void SetAllDirty()
		{
			dirtyCells.Clear();
			foreach (IntVec3 cell in map)
			{
				dirtyCells.Add(cell);
			}
			foreach (VehicleRegion region in map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionGrid.AllRegions_NoRebuild_InvalidAllowed)
			{
				SetRegionDirty(region, false);
			}
		}

		/// <summary>
		/// Notify that the walkable status at <paramref name="cell"/> has changed
		/// </summary>
		/// <param name="cell"></param>
		public void Notify_WalkabilityChanged(IntVec3 cell)
		{
			regionsToDirty.Clear();
			for (int i = 0; i < 9; i++)
			{
				IntVec3 adjCell = cell + GenAdj.AdjacentCellsAndInside[i];
				if (adjCell.InBounds(map))
				{
					VehicleRegion regionAt_NoRebuild_InvalidAllowed = map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionGrid.GetRegionAt_NoRebuild_InvalidAllowed(adjCell);
					if (regionAt_NoRebuild_InvalidAllowed != null && regionAt_NoRebuild_InvalidAllowed.valid)
					{
						regionsToDirty.Add(regionAt_NoRebuild_InvalidAllowed);
					}
				}
			}
			for (int j = 0; j < regionsToDirty.Count; j++)
			{
				SetRegionDirty(regionsToDirty[j], true);
			}
			regionsToDirty.Clear();
			if (GenGridVehicles.Walkable(cell, vehicleDef, map))
			{
				dirtyCells.Add(cell);
			}
		}

		/// <summary>
		/// Notify that <paramref name="thing"/> has spawned, potentially affecting cell status for its occupied rect
		/// </summary>
		/// <param name="thing"></param>
		public void Notify_ThingAffectingRegionsSpawned(Thing thing)
		{
			regionsToDirty.Clear();
			foreach (IntVec3 c in thing.OccupiedRect().ExpandedBy(1).ClipInsideMap(thing.Map))
			{
				VehicleRegion validRegionAt_NoRebuild = thing.Map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionGrid.GetValidRegionAt_NoRebuild(c);
				if (validRegionAt_NoRebuild != null)
				{
					regionsToDirty.Add(validRegionAt_NoRebuild);
				}
			}
			for (int i = 0; i < regionsToDirty.Count; i++)
			{
				SetRegionDirty(regionsToDirty[i], true);
			}
			regionsToDirty.Clear();
		}

		/// <summary>
		/// Notify that <paramref name="thing"/> has despawned, potentially affecting cell status for its previously occupied rect
		/// </summary>
		/// <param name="thing"></param>
		public void Notify_ThingAffectingRegionsDespawned(Thing thing)
		{
			regionsToDirty.Clear();
			VehicleRegion validRegionAt_NoRebuild = map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionGrid.GetValidRegionAt_NoRebuild(thing.Position);
			if (validRegionAt_NoRebuild != null)
			{
				regionsToDirty.Add(validRegionAt_NoRebuild);
			}
			foreach (IntVec3 c in GenAdj.CellsAdjacent8Way(thing))
			{
				if (c.InBounds(map))
				{
					VehicleRegion validRegionAt_NoRebuild2 = map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionGrid.GetValidRegionAt_NoRebuild(c);
					if (validRegionAt_NoRebuild2 != null)
					{
						regionsToDirty.Add(validRegionAt_NoRebuild2);
					}
				}
			}
			for (int i = 0; i < regionsToDirty.Count; i++)
			{
				SetRegionDirty(regionsToDirty[i], true);
			}
			regionsToDirty.Clear();
			if (thing.def.size.x == 1 && thing.def.size.z == 1)
			{
				dirtyCells.Add(thing.Position);
				return;
			}
			CellRect cellRect = thing.OccupiedRect();
			for (int j = cellRect.minZ; j <= cellRect.maxZ; j++)
			{
				for (int k = cellRect.minX; k <= cellRect.maxX; k++)
				{
					IntVec3 item = new IntVec3(k, 0, j);
					dirtyCells.Add(item);
				}
			}
		}

		/// <summary>
		/// Set <paramref name="region"/> to dirty status, marking it for update
		/// </summary>
		/// <param name="region"></param>
		/// <param name="addCellsToDirtyCells"></param>
		private void SetRegionDirty(VehicleRegion region, bool addCellsToDirtyCells = true)
		{
			if (!region.valid)
			{
				return;
			}
			region.valid = false;
			region.Room = null;
			for (int i = 0; i < region.links.Count; i++)
			{
				region.links[i].Deregister(region, vehicleDef);
			}
			region.links.Clear();
			if (addCellsToDirtyCells)
			{
				foreach (IntVec3 intVec in region.Cells)
				{
					dirtyCells.Add(intVec);
					if (DebugViewSettings.drawRegionDirties)
					{
						map.debugDrawer.FlashCell(intVec, 0f, null, 50);
					}
				}
			}
		}
	}
}
