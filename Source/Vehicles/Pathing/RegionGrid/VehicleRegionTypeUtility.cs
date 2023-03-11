using System.Collections.Generic;
using Verse;
using SmashTools;

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
		public static RegionType GetExpectedRegionType(this IntVec3 cell, VehicleMapping mapping, VehicleDef vehicleDef)
		{
			if (!cell.InBounds(mapping.map))
			{
				return RegionType.None;
			}
			if (cell.GetDoor(mapping.map) != null)
			{
				return RegionType.Portal;
			}
			if (cell.GetFence(mapping.map) != null)
			{
				return RegionType.None; //TODO - Add settings available to player for configuring fence traversal
			}
			if (!vehicleDef.WidthStandable(mapping.map, cell))
			{
				return RegionType.None;
			}
			if (mapping[vehicleDef].VehiclePathGrid.WalkableFast(cell))
			{
				return RegionType.Normal;
			}
			List<Thing> thingList = cell.GetThingList(mapping.map);
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
