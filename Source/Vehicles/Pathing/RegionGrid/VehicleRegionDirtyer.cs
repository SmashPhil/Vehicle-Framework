using System.Collections.Generic;
using Verse;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public class VehicleRegionDirtyer
	{
		private readonly Map map;

		private readonly List<IntVec3> dirtyCells = new List<IntVec3>();

		private readonly List<VehicleRegion> regionsToDirty = new List<VehicleRegion>();

		public VehicleRegionDirtyer(Map map)
		{
			this.map = map;
		}

		public bool AnyDirty
		{
			get
			{
				return dirtyCells.Count > 0;
			}
		}

		public List<IntVec3> DirtyCells
		{
			get
			{
				return dirtyCells;
			}
		}

		internal void Notify_WalkabilityChanged(IntVec3 c)
		{
			regionsToDirty.Clear();
			for (int i = 0; i < 9; i++)
			{
				IntVec3 c2 = c + GenAdj.AdjacentCellsAndInside[i];
				if (c2.InBounds(map))
				{
					VehicleRegion regionAt_NoRebuild_InvalidAllowed = map.GetCachedMapComponent<VehicleMapping>().VehicleRegionGrid.GetRegionAt_NoRebuild_InvalidAllowed(c2);
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
			if (c.Walkable(map) && !dirtyCells.Contains(c))
			{
				dirtyCells.Add(c);
			}
		}

		internal void Notify_ThingAffectingRegionsSpawned(Thing b)
		{
			regionsToDirty.Clear();
			foreach (IntVec3 c in b.OccupiedRect().ExpandedBy(1).ClipInsideMap(b.Map))
			{
				VehicleRegion validRegionAt_NoRebuild = b.Map.GetCachedMapComponent<VehicleMapping>().VehicleRegionGrid.GetValidRegionAt_NoRebuild(c);
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

		internal void Notify_ThingAffectingRegionsDespawned(Thing b)
		{
			regionsToDirty.Clear();
			VehicleRegion validRegionAt_NoRebuild = map.GetCachedMapComponent<VehicleMapping>().VehicleRegionGrid.GetValidRegionAt_NoRebuild(b.Position);
			if (validRegionAt_NoRebuild != null)
			{
				regionsToDirty.Add(validRegionAt_NoRebuild);
			}
			foreach (IntVec3 c in GenAdj.CellsAdjacent8Way(b))
			{
				if (c.InBounds(map))
				{
					VehicleRegion validRegionAt_NoRebuild2 = map.GetCachedMapComponent<VehicleMapping>().VehicleRegionGrid.GetValidRegionAt_NoRebuild(c);
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
			if (b.def.size.x == 1 && b.def.size.z == 1)
			{
				dirtyCells.Add(b.Position);
				return;
			}
			CellRect cellRect = b.OccupiedRect();
			for (int j = cellRect.minZ; j <= cellRect.maxZ; j++)
			{
				for (int k = cellRect.minX; k <= cellRect.maxX; k++)
				{
					IntVec3 item = new IntVec3(k, 0, j);
					dirtyCells.Add(item);
				}
			}
		}

		internal void SetAllClean()
		{
			for (int i = 0; i < dirtyCells.Count; i++)
			{
				map.temperatureCache.ResetCachedCellInfo(dirtyCells[i]);
			}
			dirtyCells.Clear();
		}

		private void SetRegionDirty(VehicleRegion reg, bool addCellsToDirtyCells = true)
		{
			if (!reg.valid)
			{
				return;
			}
			reg.valid = false;
			reg.Room = null;
			for (int i = 0; i < reg.links.Count; i++)
			{
				reg.links[i].Deregister(reg);
			}
			reg.links.Clear();
			if (addCellsToDirtyCells)
			{
				foreach (IntVec3 intVec in reg.Cells)
				{
					dirtyCells.Add(intVec);
					if (DebugViewSettings.drawRegionDirties)
					{
						map.debugDrawer.FlashCell(intVec, 0f, null, 50);
					}
				}
			}
		}

		internal void SetAllDirty()
		{
			dirtyCells.Clear();
			foreach (IntVec3 item in map)
			{
				dirtyCells.Add(item);
			}
			foreach (VehicleRegion reg in map.GetCachedMapComponent<VehicleMapping>().VehicleRegionGrid.AllRegions_NoRebuild_InvalidAllowed)
			{
				SetRegionDirty(reg, false);
			}
		}
	}
}
