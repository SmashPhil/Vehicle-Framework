using Verse;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Region and room retrieval helper methods
	/// </summary>
	public static class VehicleRegionAndRoomQuery
	{
		public static VehicleRegion RegionAt(IntVec3 cell, Map map, VehicleDef vehicleDef, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			return RegionAt(cell, map.GetCachedMapComponent<VehicleMapping>(), vehicleDef, allowedRegionTypes);
		}

		/// <summary>
		/// Retrieve region at <paramref name="cell"/> for <paramref name="vehicleDef"/>
		/// </summary>
		public static VehicleRegion RegionAt(IntVec3 cell, VehicleMapping mapping, VehicleDef vehicleDef, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			if (!cell.InBounds(mapping.map))
			{
				return null;
			}
			VehicleRegion validRegionAt = mapping[vehicleDef].VehicleRegionGrid.GetValidRegionAt(cell);
			if (validRegionAt != null && allowedRegionTypes.HasFlag(validRegionAt.type))
			{
				return validRegionAt;
			}
			return null;
		}

		/// <summary>
		/// Get region at <paramref name="thing"/>'s position for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="thing"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="allowedRegiontypes"></param>
		public static VehicleRegion GetRegion(this Thing thing, VehicleDef vehicleDef, RegionType allowedRegiontypes = RegionType.Set_Passable)
		{
			if (!thing.Spawned)
			{
				return null;
			}
			return !thing.Spawned ? null : RegionAt(thing.Position, thing.Map, vehicleDef, allowedRegiontypes);
		}

		/// <summary>
		/// Get room at <paramref name="cell"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="map"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="allowedRegionTypes"></param>
		public static VehicleRoom RoomAt(IntVec3 cell, Map map, VehicleDef vehicleDef, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			VehicleRegion region = RegionAt(cell, map, vehicleDef, allowedRegionTypes);
			return region?.Room;
		}

		/// <summary>
		/// Quick retrieval of room at <paramref name="cell"/> for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="map"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="allowedRegionTypes"></param>
		public static VehicleRoom RoomAtFast(IntVec3 cell, Map map, VehicleDef vehicleDef, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			VehicleRegion validRegionAt = map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionGrid?.GetValidRegionAt(cell);
			if (validRegionAt != null && (validRegionAt.type & allowedRegionTypes) != RegionType.None)
			{
				return validRegionAt.Room;
			}
			return null;
		}
	}
}
