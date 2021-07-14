using System.Collections.Generic;
using Verse;
using Vehicles.AI;

namespace Vehicles
{
	/// <summary>
	/// RegionType helper methods specific to VehicleDefs
	/// </summary>
	public static class VehicleRegionTypeUtility
	{
		/// <summary>
		/// Getter for expected region type at <paramref name="cell"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="map"></param>
		/// <param name="vehicleDef"></param>
		public static RegionType GetExpectedRegionType(this IntVec3 cell, Map map, VehicleDef vehicleDef)
		{
			if (!cell.InBounds(map))
			{
				return RegionType.None;
			}
			if (cell.GetDoor(map) != null)
			{
				return RegionType.Portal;
			}
			if (cell.GetFence(map) != null)
			{
				return RegionType.Fence;
			}
			if (GenGridVehicles.Walkable(cell, vehicleDef, map))
			{
				return RegionType.Normal;
			}
			List<Thing> thingList = cell.GetThingList(map);
			for (int i = 0; i < thingList.Count; i++)
			{
				if (thingList[i].def.Fillage == FillCategory.Full)
				{
					return RegionType.None;
				}
			}
			return RegionType.ImpassableFreeAirExchange;
		}
	}
}
