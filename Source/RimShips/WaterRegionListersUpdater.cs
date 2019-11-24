using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimShips.Build;
using RimShips.Defs;
using RimShips.AI;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips
{
    public static class WaterRegionListersUpdater
    {
        public static void DeregisterInRegions(Thing thing, Map map)
        {
            ThingDef def = thing.def;
            if (!ListerThings.EverListable(def, ListerThingsUse.Region))
                return;
            WaterRegionListersUpdater.GetTouchableRegions(thing, map, WaterRegionListersUpdater.tmpRegions, true);
            for(int i = 0; i < WaterRegionListersUpdater.tmpRegions.Count; i++)
            {
                ListerThings listerThings = WaterRegionListersUpdater.tmpRegions[i].ListerThings;
                if(listerThings.Contains(thing))
                {
                    listerThings.Remove(thing);
                }
            }
            WaterRegionListersUpdater.tmpRegions.Clear();
        }

        public static void RegisterInRegions(Thing thing, Map map)
        {
            ThingDef def = thing.def;
            if (!ListerThings.EverListable(def, ListerThingsUse.Region))
                return;
            WaterRegionListersUpdater.GetTouchableRegions(thing, map, WaterRegionListersUpdater.tmpRegions, false);
            for(int i = 0; i < WaterRegionListersUpdater.tmpRegions.Count; i++)
            {
                ListerThings listerThings = WaterRegionListersUpdater.tmpRegions[i].ListerThings;
                if(!listerThings.Contains(thing))
                {
                    listerThings.Add(thing);
                }
            }
            WaterRegionListersUpdater.tmpRegions.Clear();
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
                    WaterRegionListersUpdater.RegisterInRegions(thing, map);
                }
            }
        }

        public static void GetTouchableRegions(Thing thing, Map map, List<WaterRegion> outRegions, bool allowAdjacenttEvenIfCantTouch = false)
        {
            outRegions.Clear();
            CellRect cellRect = thing.OccupiedRect();
            CellRect cellRect2 = cellRect;
            if(WaterRegionListersUpdater.CanRegisterInAdjacentRegions(thing))
            {
                cellRect2 = cellRect2.ExpandedBy(1);
            }
            CellRect.CellRectIterator iterator = cellRect2.GetIterator();
            while(!iterator.Done())
            {
                IntVec3 intVec = iterator.Current;
                if(intVec.InBoundsShip(map))
                {
                    WaterRegion validRegionAt_NoRebuild = MapExtensionUtility.GetExtensionToMap(map).getWaterRegionGrid.GetValidRegionAt_NoRebuild(intVec);
                    if(!(validRegionAt_NoRebuild is null) && validRegionAt_NoRebuild.type.Passable() && !outRegions.Contains(validRegionAt_NoRebuild))
                    {
                        if(cellRect.Contains(intVec))
                        {
                            outRegions.Add(validRegionAt_NoRebuild);
                        }
                        else if(allowAdjacenttEvenIfCantTouch || ShipReachabilityImmediate.CanReachImmediateShip(intVec, thing, map, PathEndMode.Touch, null))
                        {
                            outRegions.Add(validRegionAt_NoRebuild);
                        }
                    }
                }
                iterator.MoveNext();
            }
        }

        private static bool CanRegisterInAdjacentRegions(Thing thing)
        {
            return true;
        }

        private static List<WaterRegion> tmpRegions = new List<WaterRegion>();
    }
}
