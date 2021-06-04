using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public sealed class VehicleRoom
	{
		public sbyte mapIndex = -1;
		public int ID = -16161616;

		public int lastChangeTick = -1;

		private int numRegionsTouchingMapEdge;

		public bool isPrisonCell;

		private int cachedCellCount = -1;

		public int newOrReusedRoomGroupIndex = -1;

		private static int nextRoomID;

		private readonly HashSet<VehicleRoom> uniqueNeighborsSet = new HashSet<VehicleRoom>();

		private readonly List<VehicleRoom> uniqueNeighbors = new List<VehicleRoom>();

		public Map Map => (mapIndex >= 0) ? Find.Maps[mapIndex] : null;

		public RegionType RegionType => (!Regions.Any()) ? RegionType.None : Regions[0].type;

		public List<VehicleRegion> Regions { get; } = new List<VehicleRegion>();

		public int RegionCount => Regions.Count;

		public bool IsHuge => Regions.Count > 60;

		public bool Dereferenced => Regions.Count == 0;

		public bool TouchesMapEdge => numRegionsTouchingMapEdge > 0;

		public int CellCount
		{
			get
			{
				if (cachedCellCount == -1)
				{
					cachedCellCount = 0;
					foreach (VehicleRegion region in Regions)
					{
						cachedCellCount += region.CellCount;
					}
				}
				return cachedCellCount;
			}
		}

		public List<VehicleRoom> Neighbors
		{
			get
			{
				uniqueNeighborsSet.Clear();
				uniqueNeighbors.Clear();
				foreach(VehicleRegion region in Regions)
				{
					foreach (VehicleRegion _ in region.Neighbors)
					{
						if (uniqueNeighborsSet.Add(region.Room) && region.Room != this)
						{
							uniqueNeighbors.Add(region.Room);
						}
					}
				}
				uniqueNeighborsSet.Clear();
				return uniqueNeighbors;
			}
		}

		public IEnumerable<IntVec3> Cells
		{
			get
			{
				foreach(VehicleRegion region in Regions)
				{
					foreach(IntVec3 c in region.Cells)
					{
						yield return c;
					}
				}
			}
		}
		
		public static VehicleRoom MakeNew(Map map)
		{
			VehicleRoom room = new VehicleRoom
			{
				mapIndex = (sbyte)map.Index,
				ID = nextRoomID
			};
			nextRoomID++;
			return room;
		}

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
				Map.GetCachedMapComponent<VehicleMapping>().VehicleRegionGrid.allRooms.Add(this);
			}
		}

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
				Map.GetCachedMapComponent<VehicleMapping>().VehicleRegionGrid.allRooms.Remove(this);
			}
		}

		public override int GetHashCode()
		{
			return Gen.HashCombineInt(ID, 1538478890);
		}
	}
}
