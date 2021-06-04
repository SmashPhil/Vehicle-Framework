using System.Collections.Generic;
using Verse;

namespace Vehicles
{
	public static class VehicleGridsUtility
	{
		public static VehicleRegion GetRegion(this IntVec3 loc, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			return VehicleRegionAndRoomQuery.RegionAt(loc, map, allowedRegionTypes);
		}
	}
}
