using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.CompilerServices;
using Verse;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Region dirtyer handler for recaching
	/// </summary>
	public class VehicleRegionDirtyer : VehicleRegionManager
    {
		private readonly HashSet<IntVec3> dirtyCells = new HashSet<IntVec3>();

		private readonly ConcurrentSet<VehicleRegion> regionsToDirty = new ConcurrentSet<VehicleRegion>();
		private readonly ConcurrentSet<VehicleRegion> regionsToDirtyFromWalkability = new ConcurrentSet<VehicleRegion>();

		private object dirtyCellsLock = new object();

		public VehicleRegionDirtyer(VehicleMapping mapping, VehicleDef createdFor) : base(mapping, createdFor)
		{
		}

		/// <summary>
		/// <see cref="dirtyCells"/> getter
		/// </summary>
		public IEnumerable<IntVec3> DirtyCells
		{
			get
			{
				lock (dirtyCellsLock)
				{
					foreach (IntVec3 cell in dirtyCells)
					{
						yield return cell;
					}
				}
			}
		}

		/// <summary>
		/// Any dirty cells registered
		/// </summary>
		public bool AnyDirty
		{
			get
			{
				lock (dirtyCellsLock)
				{
					return dirtyCells.Count > 0;
				}
			}
		}

		/// <summary>
		/// Clear all dirtyed cells
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void SetAllClean()
		{
			lock (dirtyCellsLock)
			{
				dirtyCells.Clear();
			}
		}

		/// <summary>
		/// Set all cells and regions to dirty status
		/// </summary>
		internal void SetAllDirty()
		{
			lock (dirtyCellsLock)
			{
				dirtyCells.Clear();
				foreach (IntVec3 cell in mapping.map)
				{
					dirtyCells.Add(cell);
				}
			}

			foreach (VehicleRegion region in mapping[createdFor].VehicleRegionGrid.AllRegions_NoRebuild_InvalidAllowed)
			{
				SetRegionDirty(region, addCellsToDirtyCells: false);
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
						SetRegionDirty(regionAt_NoRebuild_InvalidAllowed);
					}
				}
			}
			if (GenGridVehicles.Walkable(cell, createdFor, mapping.map))
			{
				lock (dirtyCellsLock)
				{
					dirtyCells.Add(cell);
				}
			}
			regionsToDirtyFromWalkability.Clear();
		}

		public void Notify_ThingAffectingRegionsSpawned(CellRect occupiedRect)
		{
			regionsToDirty.Clear();
			foreach (IntVec3 cell in occupiedRect.ExpandedBy(createdFor.SizePadding + 1).ClipInsideMap(mapping.map))
			{
				VehicleRegion validRegionAt_NoRebuild = mapping[createdFor].VehicleRegionGrid.GetValidRegionAt_NoRebuild(cell);
				if (validRegionAt_NoRebuild != null)
				{
					regionsToDirty.Add(validRegionAt_NoRebuild);
				}
			}
			foreach (VehicleRegion vehicleRegion in regionsToDirty.Keys)
			{
				SetRegionDirty(vehicleRegion);
			}
			regionsToDirty.Clear();
		}

		public void Notify_ThingAffectingRegionsDespawned(CellRect occupiedRect)
		{
			regionsToDirty.Clear();
			foreach (IntVec3 cell in occupiedRect.ExpandedBy(createdFor.SizePadding + 1).ClipInsideMap(mapping.map))
			{
				if (cell.InBounds(mapping.map))
				{
					VehicleRegion validRegionAt_NoRebuild2 = mapping[createdFor].VehicleRegionGrid.GetValidRegionAt_NoRebuild(cell);
					if (validRegionAt_NoRebuild2 != null)
					{
						regionsToDirty.Add(validRegionAt_NoRebuild2);
					}
				}
			}
			foreach (VehicleRegion vehicleRegion in regionsToDirty.Keys)
			{
				SetRegionDirty(vehicleRegion);
			}
			regionsToDirty.Clear();

			lock (dirtyCellsLock)
			{
				foreach (IntVec3 cell in occupiedRect)
				{
					dirtyCells.Add(cell);
				}
			}
		}

		/// <summary>
		/// Set <paramref name="region"/> to dirty status, marking it for update
		/// </summary>
		/// <param name="region"></param>
		/// <param name="addCellsToDirtyCells"></param>
		private void SetRegionDirty(VehicleRegion region, bool addCellsToDirtyCells = true, bool dirtyLinkedRegions = true)
		{
			try
			{
				if (!region.valid)
				{
					return;
				}
				region.valid = false;
				region.Room = null;
				foreach (VehicleRegionLink regionLink in region.links.Keys)
				{
					VehicleRegion otherRegion = regionLink.Deregister(region, createdFor);
					if (otherRegion != null && dirtyLinkedRegions)
					{
						SetRegionDirty(otherRegion, addCellsToDirtyCells: addCellsToDirtyCells, dirtyLinkedRegions: false);
					}
				}
				region.links.Clear();
				region.ClearWeights();
				if (addCellsToDirtyCells)
				{
					lock (dirtyCellsLock)
					{
						foreach (IntVec3 intVec in region.Cells)
						{
							dirtyCells.Add(intVec);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown in SetRegionDirty. Exception={ex}");
			}
		}
	}
}
