using System.Collections.Generic;
using Verse;

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
