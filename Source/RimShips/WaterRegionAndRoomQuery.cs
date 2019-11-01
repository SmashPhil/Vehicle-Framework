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
    public static class WaterRegionAndRoomQuery
    {
        public static WaterRegion RegionAt(IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
        {
            Log.Message("in bounds? " + (GenGridShips.InBounds(c, map)));
            if (!GenGridShips.InBounds(c, map))
                return null;
            WaterRegion validRegionAt = MapExtensionUtility.GetExtensionToMap(map).getWaterRegionGrid.GetValidRegionAt(c);
            Log.Message("is null? " + (validRegionAt is null));
            return !(validRegionAt is null) && (validRegionAt.type & allowedRegionTypes) != RegionType.None ? validRegionAt : null;
        }

        public static WaterRegion GetRegion(this Thing thing, RegionType allowedRegiontypes = RegionType.Set_Passable)
        {
            if (!thing.Spawned)
                return null;
            return !thing.Spawned ? null : WaterRegionAndRoomQuery.RegionAt(thing.Position, thing.Map, allowedRegiontypes);
        }

        //RoomAt

        //RoomGroupAt

        //GetRoom

        //GetRoomGroup

        //RoomAtFast

        //RoomAtOrAdjacent


    }
}
