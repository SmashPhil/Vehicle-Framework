using System.Collections.Generic;
using Verse;
using Verse.AI;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public static class WaterRegionListersUpdater
	{
		private static readonly List<WaterRegion> tmpRegions = new List<WaterRegion>();

		public static void DeregisterInRegions(Thing thing, Map map)
		{
			ThingDef def = thing.def;
			if (!ListerThings.EverListable(def, ListerThingsUse.Region)) return;
			GetTouchableRegions(thing, map, tmpRegions, true);
			for(int i = 0; i < tmpRegions.Count; i++)
			{
				ListerThings listerThings = tmpRegions[i].ListerThings;
				if(listerThings.Contains(thing))
				{
					listerThings.Remove(thing);
				}
			}
			tmpRegions.Clear();
		}

		public static void RegisterInRegions(Thing thing, Map map)
		{
			ThingDef def = thing.def;
			if (!ListerThings.EverListable(def, ListerThingsUse.Region)) return;
			GetTouchableRegions(thing, map, tmpRegions, false);
			for(int i = 0; i < tmpRegions.Count; i++)
			{
				ListerThings listerThings = tmpRegions[i].ListerThings;
				if(!listerThings.Contains(thing))
				{
					listerThings.Add(thing);
				}
			}
			tmpRegions.Clear();
		}

		public static void RegisterAllAt(IntVec3 c, Map map, HashSet<Thing> processedThings = null)
		{
			List<Thing> thingList = c.GetThingList(map);
			int count = thingList.Count;
			for(int i = 0; i < count; i++)
			{
				Thing thing = thingList[i];
				if(processedThings is null || processedThings.Add(thing))
				{
					RegisterInRegions(thing, map);
				}
			}
		}

		public static void GetTouchableRegions(Thing thing, Map map, List<WaterRegion> outRegions, bool allowAdjacenttEvenIfCantTouch = false)
		{
			outRegions.Clear();
			CellRect cellRect = thing.OccupiedRect();
			CellRect cellRect2 = cellRect;
			if(CanRegisterInAdjacentRegions(thing))
			{
				cellRect2 = cellRect2.ExpandedBy(1);
			}
			foreach (IntVec3 intVec in cellRect2)
			{
				if (intVec.InBoundsShip(map))
				{
					WaterRegion validRegionAt_NoRebuild = map.GetCachedMapComponent<WaterMap>().WaterRegionGrid.GetValidRegionAt_NoRebuild(intVec);
					if (!(validRegionAt_NoRebuild is null) && validRegionAt_NoRebuild.type.Passable() && !outRegions.Contains(validRegionAt_NoRebuild))
					{
						if (cellRect.Contains(intVec))
						{
							outRegions.Add(validRegionAt_NoRebuild);
						}
						else if (allowAdjacenttEvenIfCantTouch || ShipReachabilityImmediate.CanReachImmediateShip(intVec, thing, map, PathEndMode.Touch, null))
						{
							outRegions.Add(validRegionAt_NoRebuild);
						}
					}
				}
			}
		}

		private static bool CanRegisterInAdjacentRegions(Thing thing)
		{
			return true;
		}
	}
}
