using System.Collections.Generic;
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
		private static readonly List<VehicleRegion> tmpRegions = new List<VehicleRegion>();

		/// <summary>
		/// Deregister <paramref name="thing"/> from nearby region
		/// </summary>
		/// <param name="thing"></param>
		/// <param name="map"></param>
		public static void DeregisterInRegions(Thing thing, Map map, VehicleDef vehicleDef)
		{
			if (!ListerThings.EverListable(thing.def, ListerThingsUse.Region))
			{
				return;
			}
			GetTouchableRegions(thing, map, vehicleDef, tmpRegions, true);
			for (int i = 0; i < tmpRegions.Count; i++)
			{
				ListerThings listerThings = tmpRegions[i].ListerThings;
				if (listerThings.Contains(thing))
				{
					listerThings.Remove(thing);
				}
			}
			tmpRegions.Clear();
		}

		/// <summary>
		/// Register <paramref name="thing"/> in nearby region
		/// </summary>
		/// <param name="thing"></param>
		/// <param name="map"></param>
		public static void RegisterInRegions(Thing thing, Map map, VehicleDef vehicleDef)
		{
			if (!ListerThings.EverListable(thing.def, ListerThingsUse.Region))
			{
				return;
			}
			GetTouchableRegions(thing, map, vehicleDef, tmpRegions, false);
			for (int i = 0; i < tmpRegions.Count; i++)
			{
				ListerThings listerThings = tmpRegions[i].ListerThings;
				if (!listerThings.Contains(thing))
				{
					listerThings.Add(thing);
				}
			}
			tmpRegions.Clear();
		}

		/// <summary>
		/// Register all things at <paramref name="cell"/>
		/// </summary>
		/// <param name="c"></param>
		/// <param name="map"></param>
		/// <param name="processedThings"></param>
		public static void RegisterAllAt(IntVec3 cell, Map map, VehicleDef vehicleDef, HashSet<Thing> processedThings = null)
		{
			List<Thing> thingList = cell.GetThingList(map);
			int count = thingList.Count;
			for (int i = 0; i < count; i++)
			{
				Thing thing = thingList[i];
				if (processedThings is null || processedThings.Add(thing))
				{
					RegisterInRegions(thing, map, vehicleDef);
				}
			}
		}

		/// <summary>
		/// Get all touchable regions for <paramref name="thing"/> on region grid associated with <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="thing"></param>
		/// <param name="map"></param>
		/// <param name="outRegions"></param>
		/// <param name="allowAdjacenttEvenIfCantTouch"></param>
		public static void GetTouchableRegions(Thing thing, Map map, VehicleDef vehicleDef, List<VehicleRegion> outRegions, bool allowAdjacenttEvenIfCantTouch = false)
		{
			outRegions.Clear();
			CellRect cellRect = thing.OccupiedRect();
			CellRect cellRect2 = cellRect;
			if (CanRegisterInAdjacentRegions(thing))
			{
				cellRect2 = cellRect2.ExpandedBy(1);
			}
			foreach (IntVec3 intVec in cellRect2)
			{
				if (intVec.InBounds(map))
				{
					VehicleMapping.VehiclePathData vehiclePathData = map.GetCachedMapComponent<VehicleMapping>()[vehicleDef];
					VehicleRegion validRegionAt_NoRebuild = vehiclePathData.VehicleRegionGrid.GetValidRegionAt_NoRebuild(intVec);
					if (validRegionAt_NoRebuild != null && validRegionAt_NoRebuild.type.Passable() && !outRegions.Contains(validRegionAt_NoRebuild))
					{
						if (cellRect.Contains(intVec))
						{
							outRegions.Add(validRegionAt_NoRebuild);
						}
						else if (allowAdjacenttEvenIfCantTouch || VehicleReachabilityImmediate.CanReachImmediateVehicle(intVec, thing, map, vehicleDef, PathEndMode.Touch))
						{
							outRegions.Add(validRegionAt_NoRebuild);
						}
					}
				}
			}
		}

		/// <summary>
		/// Tmp method in case vanilla ever modifies this behavior to be specific to ThingDefs
		/// </summary>
		/// <param name="thing"></param>
		/// <returns></returns>
		private static bool CanRegisterInAdjacentRegions(Thing thing)
		{
			return true;
		}
	}
}
