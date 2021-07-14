using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	/// <summary>
	/// Vehicle specific room handler
	/// </summary>
	public sealed class VehicleRoom
	{
		private static int nextRoomID;

		public sbyte mapIndex = -1;
		public int ID = -16161616;

		private readonly VehicleDef vehicleDef;

		public int lastChangeTick = -1;
		private int numRegionsTouchingMapEdge;
		private int cachedCellCount = -1;

		private readonly HashSet<VehicleRoom> uniqueNeighborsSet = new HashSet<VehicleRoom>();
		private readonly List<VehicleRoom> uniqueNeighbors = new List<VehicleRoom>();

		public VehicleRoom(VehicleDef vehicleDef)
		{
			this.vehicleDef = vehicleDef;
		}

		/// <summary>
		/// Map getter with fallback
		/// </summary>
		public Map Map => (mapIndex >= 0) ? Find.Maps[mapIndex] : null;

		/// <summary>
		/// Region type with fallback
		/// </summary>
		public RegionType RegionType => Regions.NullOrEmpty() ? RegionType.None : Regions[0].type;

		/// <summary>
		/// Region getter for regions contained within room
		/// </summary>
		public List<VehicleRegion> Regions { get; } = new List<VehicleRegion>();

		/// <summary>
		/// Region count
		/// </summary>
		public int RegionCount => Regions.Count;

		/// <summary>
		/// Room touches map edge
		/// </summary>
		public bool TouchesMapEdge => numRegionsTouchingMapEdge > 0;
		
		/// <summary>
		/// Create new room for <paramref name="vehicleDef"/>
		/// </summary>
		/// <param name="map"></param>
		/// <param name="vehicleDef"></param>
		public static VehicleRoom MakeNew(Map map, VehicleDef vehicleDef)
		{
			VehicleRoom room = new VehicleRoom(vehicleDef)
			{
				mapIndex = (sbyte)map.Index,
				ID = nextRoomID
			};
			nextRoomID++;
			return room;
		}

		/// <summary>
		/// Add region to room
		/// </summary>
		/// <param name="region"></param>
		public void AddRegion(VehicleRegion region)
		{
			if (Regions.Contains(region))
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to add the same region twice to Room. region=",
					region,
					", room=",
					this
				}));
				return;
			}
			Regions.Add(region);
			if (region.touchesMapEdge)
			{
				numRegionsTouchingMapEdge++;
			}
			if (Regions.Count == 1)
			{
				Map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionGrid.allRooms.Add(this);
			}
		}

		/// <summary>
		/// Remove region from room
		/// </summary>
		/// <param name="r"></param>
		public void RemoveRegion(VehicleRegion r)
		{
			if (!Regions.Contains(r))
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to remove region from Room but this region is not here. region=",
					r,
					", room=",
					this
				}));
				return;
			}
			Regions.Remove(r);
			if (r.touchesMapEdge)
			{
				numRegionsTouchingMapEdge--;
			}
			if (Regions.Count == 0)
			{
				Map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionGrid.allRooms.Remove(this);
			}
		}

		/// <summary>
		/// ID based hashcode
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return Gen.HashCombineInt(ID, vehicleDef.GetHashCode());
		}
	}
}
