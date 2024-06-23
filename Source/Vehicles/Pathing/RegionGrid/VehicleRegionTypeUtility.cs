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
		public static RegionType GetExpectedRegionType(IntVec3 cell, VehicleMapping mapping, VehicleDef vehicleDef)
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
			if (!VerifyCardinalCellSpace(cell, mapping, vehicleDef))
			{
				return RegionType.None;
			}
			if (mapping[vehicleDef].VehiclePathGrid.WalkableFast(cell))
			{
				return RegionType.Normal;
			}
			return RegionType.None;

			//List<Thing> thingList = cell.GetThingList(mapping.map);
			//for (int i = 0; i < thingList.Count; i++)
			//{
			//	if (thingList[i].def.Fillage == FillCategory.Full)
			//	{
			//		return RegionType.None;
			//	}
			//}
			//return RegionType.ImpassableFreeAirExchange;
		}

		//TODO - Account for non-uniform combinations (eg. Y shape)
		private static bool VerifyCardinalCellSpace(IntVec3 cell, VehicleMapping mapping, VehicleDef vehicleDef)
		{
			if (vehicleDef.Size.x % 2 == 0 || vehicleDef.Size.z % 2 == 0)
			{
				int padding = vehicleDef.SizePadding;
				IntVec3 north = new IntVec3(cell.x, 0, cell.z + padding);
				IntVec3 east = new IntVec3(cell.x + padding, 0, cell.z);
				IntVec3 south = new IntVec3(cell.x, 0, cell.z - padding);
				IntVec3 west = new IntVec3(cell.x - padding, 0, cell.z);
				IntVec3 northEast = new IntVec3(cell.x + padding, 0, cell.z + padding);
				IntVec3 southEast = new IntVec3(cell.x + padding, 0, cell.z - padding);
				IntVec3 southWest = new IntVec3(cell.x - padding, 0, cell.z - padding);
				IntVec3 northWest = new IntVec3(cell.x - padding, 0, cell.z + padding);
				if (!vehicleDef.WidthStandable(mapping.map, north) && !vehicleDef.WidthStandable(mapping.map, south))
				{
					return false;
				}
				else if (!vehicleDef.WidthStandable(mapping.map, east) && !vehicleDef.WidthStandable(mapping.map, west))
				{
					return false;
				}
				else if (!vehicleDef.WidthStandable(mapping.map, northEast) && !vehicleDef.WidthStandable(mapping.map, southWest))
				{
					return false;
				}
				else if (!vehicleDef.WidthStandable(mapping.map, southEast) && !vehicleDef.WidthStandable(mapping.map, northWest))
				{
					return false;
				}
			}
			return true;
		}
	}
}
