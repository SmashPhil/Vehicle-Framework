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
using RimShips.AI;
using RimShips.Defs;
using RimShips.Build;
using RimShips.Jobs;
using RimShips.Lords;
using RimShips.UI;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips
{
    public static class WaterRegionTypeUtility
    {
        public static bool IsOneCellRegion(this RegionType regionType)
        {
            return regionType is RegionType.Portal;
        }

        public static bool AllowsMultipleRegionsPerRoom(this RegionType regionType)
        {
            return regionType != RegionType.Portal;
        }

        public static RegionType GetExpectedRegionType(this IntVec3 c, Map map)
        {
            if(!c.InBoundsShip(map))
                return RegionType.None;
            if(!(c.GetDoor(map) is null))
                return RegionType.Portal;
            if(GenGridShips.Walkable(c, MapExtensionUtility.GetExtensionToMap(map)))
                return RegionType.Normal;
            return RegionType.ImpassableFreeAirExchange;
        }

        public static RegionType GetRegionType(this IntVec3 c, Map map)
        {
            //Future Implementation?
            return RegionType.None;
        }

        public static bool Passable(this RegionType regionType)
        {
            return (regionType & RegionType.Set_Passable) != RegionType.None;
        }
    }
}
