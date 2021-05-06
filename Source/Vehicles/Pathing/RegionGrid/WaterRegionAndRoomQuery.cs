using Vehicles.AI;
using Verse;
using SmashTools;

namespace Vehicles
{
	public static class WaterRegionAndRoomQuery
	{
		public static WaterRegion RegionAt(IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			if (!c.InBoundsShip(map)) return null;
			WaterRegion validRegionAt = map.GetCachedMapComponent<WaterMap>().WaterRegionGrid.GetValidRegionAt(c);
			return !(validRegionAt is null) && (validRegionAt.type & allowedRegionTypes) != RegionType.None ? validRegionAt : null;
		}

		public static WaterRegion GetRegion(this Thing thing, RegionType allowedRegiontypes = RegionType.Set_Passable)
		{
			if (!thing.Spawned) return null;
			return !thing.Spawned ? null : RegionAt(thing.Position, thing.Map, allowedRegiontypes);
		}

		public static WaterRoom RoomAt(IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			WaterRegion region = RegionAt(c, map, allowedRegionTypes);
			return region?.Room;
		}

		//RoomGroup

		public static WaterRoom GetRoom(this Thing thing, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			if (!thing.Spawned) return null;
			return RoomAt(thing.Position, thing.Map, allowedRegionTypes);
		}

		//GetRoomGroup

		public static WaterRoom RoomAtFast(IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			WaterRegion validRegionAt = map.GetCachedMapComponent<WaterMap>()?.WaterRegionGrid?.GetValidRegionAt(c);
			if (!(validRegionAt is null) && (validRegionAt.type & allowedRegionTypes) != RegionType.None)
			{
				return validRegionAt.Room;
			}
			return null;
		}

		public static WaterRoom RoomAtOrAdjacent(IntVec3 c, Map map, RegionType allowedRegionTypes = RegionType.Set_Passable)
		{
			WaterRoom room = RoomAt(c, map, allowedRegionTypes);
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
