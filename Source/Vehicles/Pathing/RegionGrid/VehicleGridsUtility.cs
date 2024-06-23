using System;
using System.Collections.Generic;
using Verse;

namespace Vehicles
{
	/// <summary>
	/// Region grid related helper methods
	/// </summary>
	public static class VehicleGridsUtility
	{
		/// <summary>
		/// Retrieve region at <paramref name="loc"/>
		/// </summary>
		/// <param name="loc"></param>
		/// <param name="map"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="allowedRegionTypes"></param>
		[Obsolete("Call VehicleRegionAndRoomQuery.RegionAt instead.")]
		public static VehicleRegion GetRegion(this IntVec3 loc, Map map, VehicleDef vehicleDef, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			return VehicleRegionAndRoomQuery.RegionAt(loc, map, vehicleDef, allowedRegionTypes);
		}
	}
}
