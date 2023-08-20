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
		private readonly VehicleMapping mapping;
		private readonly VehicleDef createdFor;

		private readonly HashSet<IntVec3> dirtyCells = new HashSet<IntVec3>();

		private readonly List<VehicleRegion> regionsToDirty = new List<VehicleRegion>();
		private readonly List<VehicleRegion> regionsToDirtyFromWalkability = new List<VehicleRegion>();

		public VehicleRegionDirtyer(VehicleMapping mapping, VehicleDef createdFor)
		{
			this.mapping = mapping;
			this.createdFor = createdFor;
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
			foreach (IntVec3 cell in mapping.map)
			{
				dirtyCells.Add(cell);
			}
			foreach (VehicleRegion region in mapping[createdFor].VehicleRegionGrid.AllRegions_NoRebuild_InvalidAllowed)
			{
				SetRegionDirty(region, false);
			}
		}

		/// <summary>
		/// Notify that the walkable status at <paramref name="cell"/> has changed
		/// </summary>
		/// <remarks>Uses different static list, may be called from other threads than DedicatedThread for regions</remarks>
		/// <param name="cell"></param>
		public void Notify_WalkabilityChanged(IntVec3 cell)
		{
			regionsToDirtyFromWalkability.Clear();
			for (int i = 0; i < 9; i++)
			{
				IntVec3 adjCell = cell + GenAdj.AdjacentCellsAndInside[i];
				if (adjCell.InBounds(mapping.map))
				{
					VehicleRegion regionAt_NoRebuild_InvalidAllowed = mapping[createdFor].VehicleRegionGrid.GetRegionAt_NoRebuild_InvalidAllowed(adjCell);
					if (regionAt_NoRebuild_InvalidAllowed != null && regionAt_NoRebuild_InvalidAllowed.valid)
					{
						regionsToDirtyFromWalkability.Add(regionAt_NoRebuild_InvalidAllowed);
					}
				}
			}
			for (int j = 0; j < regionsToDirtyFromWalkability.Count; j++)
			{
				SetRegionDirty(regionsToDirtyFromWalkability[j], true);
			}
			if (GenGridVehicles.Walkable(cell, createdFor, mapping.map))
			{
				dirtyCells.Add(cell);
			}
			regionsToDirtyFromWalkability.Clear();
		}

		/// <summary>
		/// Notify that <paramref name="thing"/> has spawned, potentially affecting cell status for its occupied rect
		/// </summary>
		/// <param name="thing"></param>
		public void Notify_ThingAffectingRegionsSpawned(Thing thing)
		{
			regionsToDirty.Clear();
			foreach (IntVec3 cell in thing.OccupiedRect().ExpandedBy(1).ClipInsideMap(mapping.map))
			{
				VehicleRegion validRegionAt_NoRebuild = mapping[createdFor].VehicleRegionGrid.GetValidRegionAt_NoRebuild(cell);
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
			VehicleRegion validRegionAt_NoRebuild = mapping[createdFor].VehicleRegionGrid.GetValidRegionAt_NoRebuild(thing.Position);
			if (validRegionAt_NoRebuild != null)
			{
				regionsToDirty.Add(validRegionAt_NoRebuild);
			}
			IntVec2 sizeWithPadding = thing.def.size + new IntVec2(createdFor.SizePadding * 2, createdFor.SizePadding * 2); //Doubled to account for opposite directions (N to S, E to W)
			foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(thing.Position, thing.Rotation, sizeWithPadding))
			{
				if (cell.InBounds(mapping.map))
				{
					//mapping.map.debugDrawer.FlashCell(cell, 0);
					VehicleRegion validRegionAt_NoRebuild2 = mapping[createdFor].VehicleRegionGrid.GetValidRegionAt_NoRebuild(cell);
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
				region.links[i].Deregister(region, createdFor);
			}
			region.links.Clear();
			region.weights.Clear();
			if (addCellsToDirtyCells)
			{
				foreach (IntVec3 intVec in region.Cells)
				{
					dirtyCells.Add(intVec);
				}
			}
		}
	}
}
