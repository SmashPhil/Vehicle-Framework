using System;
using System.Collections.Generic;
using System.Threading;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Region Lister utility class
	/// </summary>
	public static class VehicleRegionListersUpdater
	{
		private static ThreadLocal<List<VehicleRegion>> regions = new ThreadLocal<List<VehicleRegion>>(() => new List<VehicleRegion>());

		/// <summary>
		/// Deregister <paramref name="thing"/> from nearby region
		/// </summary>
		/// <param name="thing"></param>
		/// <param name="mapping"></param>
		public static void DeregisterInRegions(Thing thing, VehicleMapping mapping, VehicleDef vehicleDef)
		{
			return;
			if (!ConcurrentListerThings.EverListable(thing.def, ListerThingsUse.Region))
			{
				return;
			}

			GetTouchableRegions(thing, mapping, vehicleDef, regions.Value, true);
			for (int i = 0; i < regions.Value.Count; i++)
			{
				ConcurrentListerThings listerThings = regions.Value[i].ListerThings;
				if (listerThings.Contains(thing))
				{
					listerThings.Remove(thing);
				}
			}
			regions.Value.Clear();
		}

		/// <summary>
		/// Register <paramref name="thing"/> in nearby region
		/// </summary>
		/// <param name="thing"></param>
		/// <param name="mapping"></param>
		public static void RegisterInRegions(Thing thing, VehicleMapping mapping, VehicleDef vehicleDef)
		{
			return;
			if (!ConcurrentListerThings.EverListable(thing.def, ListerThingsUse.Region))
			{
				return;
			}

			GetTouchableRegions(thing, mapping, vehicleDef, regions.Value, false);
			foreach (VehicleRegion vehicleRegion in regions.Value)
			{
				ConcurrentListerThings listerThings = vehicleRegion.ListerThings;
				if (!listerThings.Contains(thing))
				{
					listerThings.Add(thing);
				}
			}
			regions.Value.Clear();
		}

		/// <summary>
		/// Register all things at <paramref name="cell"/>
		/// </summary>
		public static void RegisterAllAt(IntVec3 cell, VehicleMapping mapping, VehicleDef vehicleDef, HashSet<Thing> processedThings = null)
		{
			List<Thing> thingList = cell.GetThingList(mapping.map);
			int count = thingList.Count;
			for (int i = 0; i < count; i++)
			{
				Thing thing = thingList[i];
				if (processedThings is null || processedThings.Add(thing))
				{
					RegisterInRegions(thing, mapping, vehicleDef);
				}
			}
		}

		/// <summary>
		/// Get all touchable regions for <paramref name="thing"/> on region grid associated with <paramref name="vehicleDef"/>
		/// </summary>
		public static void GetTouchableRegions(Thing thing, VehicleMapping mapping, VehicleDef vehicleDef, List<VehicleRegion> outRegions, bool allowAdjacenttEvenIfCantTouch = false)
		{
			outRegions.Clear();
			CellRect cellRect = thing.OccupiedRect().ExpandedBy(1);

			foreach (IntVec3 intVec in cellRect)
			{
				if (intVec.InBounds(mapping.map))
				{
					VehicleMapping.VehiclePathData vehiclePathData = mapping[vehicleDef];
					VehicleRegion region = vehiclePathData.VehicleRegionGrid.GetValidRegionAt_NoRebuild(intVec);
					
					if (region != null && region.type.Passable() && !outRegions.Contains(region))
					{
						if (cellRect.Contains(intVec))
						{
							outRegions.Add(region);
						}
						else if (allowAdjacenttEvenIfCantTouch || VehicleReachabilityImmediate.CanReachImmediateVehicle(intVec, thing, mapping.map, vehicleDef, PathEndMode.Touch))
						{
							outRegions.Add(region);
						}
					}
				}
			}
		}
	}
}
