using Vehicles.AI;
using Verse;

namespace Vehicles
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
