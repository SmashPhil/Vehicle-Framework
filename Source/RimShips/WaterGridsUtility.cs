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
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips
{
    public static class WaterGridsUtility
    {
        public static WaterRegion GetRegion(this IntVec3 loc, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
        {
            return WaterRegionAndRoomQuery.RegionAt(loc, map, allowedRegionTypes);
        }

        //GetRoom

        //GetRoomGroup

        //GetRoomOrAdjacent

        public static List<Thing> GetThingList(this IntVec3 c, Map map)
        {
            return map.thingGrid.ThingsListAt(c);
        }

        public static bool Fogged(this Thing t)
        {
            return t.Map.fogGrid.IsFogged(t.Position);
        }
    }
}
