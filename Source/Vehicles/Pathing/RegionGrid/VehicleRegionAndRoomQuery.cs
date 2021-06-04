using Vehicles.AI;
using Verse;
using SmashTools;

namespace Vehicles
{
	public static class VehicleRegionAndRoomQuery
	{
		public static VehicleRegion RegionAt(IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			if (!c.InBoundsShip(map)) return null;
			VehicleRegion validRegionAt = map.GetCachedMapComponent<VehicleMapping>().VehicleRegionGrid.GetValidRegionAt(c);
			return !(validRegionAt is null) && (validRegionAt.type & allowedRegionTypes) != RegionType.None ? validRegionAt : null;
		}

		public static VehicleRegion GetRegion(this Thing thing, RegionType allowedRegiontypes = RegionType.Set_Passable)
		{
			if (!thing.Spawned) return null;
			return !thing.Spawned ? null : RegionAt(thing.Position, thing.Map, allowedRegiontypes);
		}

		public static VehicleRoom RoomAt(IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			VehicleRegion region = RegionAt(c, map, allowedRegionTypes);
			return region?.Room;
		}

		//RoomGroup

		public static VehicleRoom GetRoom(this Thing thing, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			if (!thing.Spawned) return null;
			return RoomAt(thing.Position, thing.Map, allowedRegionTypes);
		}

		//GetRoomGroup

		public static VehicleRoom RoomAtFast(IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			VehicleRegion validRegionAt = map.GetCachedMapComponent<VehicleMapping>()?.VehicleRegionGrid?.GetValidRegionAt(c);
			if (!(validRegionAt is null) && (validRegionAt.type & allowedRegionTypes) != RegionType.None)
			{
				return validRegionAt.Room;
			}
			return null;
		}

		public static VehicleRoom RoomAtOrAdjacent(IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			VehicleRoom room = RoomAt(c, map, allowedRegionTypes);
			if (!(room is null))
			{
				return room;
			}
			for(int i = 0; i < 8; i++)
			{
				IntVec3 c2 = c + GenAdj.AdjacentCells[i];
				room = RoomAt(c2, map, allowedRegionTypes);
				if (!(room is null))
				{
					return room;
				}
			}
			return room;
		}
	}
}
