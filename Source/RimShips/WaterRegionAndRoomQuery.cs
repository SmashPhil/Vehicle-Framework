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
            if (!c.InBoundsShip(map))
                return null;
            WaterRegion validRegionAt = MapExtensionUtility.GetExtensionToMap(map).getWaterRegionGrid.GetValidRegionAt(c);
            return !(validRegionAt is null) && (validRegionAt.type & allowedRegionTypes) != RegionType.None ? validRegionAt : null;
        }

        public static WaterRegion GetRegion(this Thing thing, RegionType allowedRegiontypes = RegionType.Set_Passable)
        {
            if (!thing.Spawned)
                return null;
            return !thing.Spawned ? null : WaterRegionAndRoomQuery.RegionAt(thing.Position, thing.Map, allowedRegiontypes);
        }

        public static WaterRoom RoomAt(IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
        {
            WaterRegion region = WaterRegionAndRoomQuery.RegionAt(c, map, allowedRegionTypes);
            return region is null ? null : region.Room;
        }

        //RoomGroup

        public static WaterRoom GetRoom(this Thing thing, RegionType allowedRegionTypes = RegionType.Set_Passable)
        {
            if (!thing.Spawned)
                return null;
            return WaterRegionAndRoomQuery.RoomAt(thing.Position, thing.Map, allowedRegionTypes);
        }

        //GetRoomGroup

        public static WaterRoom RoomAtFast(IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
        {
            WaterRegion validRegionAt = MapExtensionUtility.GetExtensionToMap(map)?.getWaterRegionGrid?.GetValidRegionAt(c);
            if(!(validRegionAt is null) && (validRegionAt.type & allowedRegionTypes) != RegionType.None)
                return validRegionAt.Room;
            return null;
        }

        public static WaterRoom RoomAtOrAdjacent(IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
        {
            WaterRoom room = WaterRegionAndRoomQuery.RoomAt(c, map, allowedRegionTypes);
            if (!(room is null))
                return room;
            for(int i = 0; i < 8; i++)
            {
                IntVec3 c2 = c + GenAdj.AdjacentCells[i];
                room = WaterRegionAndRoomQuery.RoomAt(c2, map, allowedRegionTypes);
                if (!(room is null))
                    return room;
            }
            return room;
        }
    }
}
