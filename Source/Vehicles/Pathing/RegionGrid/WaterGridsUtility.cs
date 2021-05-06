using System.Collections.Generic;
using Verse;

namespace Vehicles
{
	public static class WaterGridsUtility
	{
		public static WaterRegion GetRegion(this IntVec3 loc, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			return WaterRegionAndRoomQuery.RegionAt(loc, map, allowedRegionTypes);
		}
	}
}
