using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public sealed class WaterRoom
	{
		public sbyte mapIndex = -1;
		public int ID = -16161616;

		public int lastChangeTick = -1;

		private int numRegionsTouchingMapEdge;

		public bool isPrisonCell;

		private int cachedCellCount = -1;

		public int newOrReusedRoomGroupIndex = -1;

		private static int nextRoomID;

		private readonly HashSet<WaterRoom> uniqueNeighborsSet = new HashSet<WaterRoom>();

		private readonly List<WaterRoom> uniqueNeighbors = new List<WaterRoom>();

		public Map Map => (mapIndex >= 0) ? Find.Maps[mapIndex] : null;

		public RegionType RegionType => (!Regions.Any()) ? RegionType.None : Regions[0].type;

		public List<WaterRegion> Regions { get; } = new List<WaterRegion>();

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
					foreach (WaterRegion region in Regions)
					{
						cachedCellCount += region.CellCount;
					}
				}
				return cachedCellCount;
			}
		}

		public List<WaterRoom> Neighbors
		{
			get
			{
				uniqueNeighborsSet.Clear();
				uniqueNeighbors.Clear();
				foreach(WaterRegion region in Regions)
				{
					foreach (WaterRegion _ in region.Neighbors)
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
				foreach(WaterRegion region in Regions)
				{
					foreach(IntVec3 c in region.Cells)
					{
						yield return c;
					}
				}
				yield break;
			}
		}
		
		public static WaterRoom MakeNew(Map map)
		{
			WaterRoom room = new WaterRoom
			{
				mapIndex = (sbyte)map.Index,
				ID = nextRoomID
			};
			nextRoomID++;
			return room;
		}

		public void AddRegion(WaterRegion r)
		{
			if (Regions.Contains(r))
			{
				Log.Error(string.Concat(new object[]
				{
					"Tried to add the same region twice to Room. region=",
					r,
					", room=",
					this
				}));
				return;
			}
			Regions.Add(r);
			if (r.touchesMapEdge)
			{
				numRegionsTouchingMapEdge++;
			}
			if (Regions.Count == 1)
			{
				Map.GetCachedMapComponent<WaterMap>().WaterRegionGrid.allRooms.Add(this);
			}
		}

		public void RemoveRegion(WaterRegion r)
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
				Map.GetCachedMapComponent<WaterMap>().WaterRegionGrid.allRooms.Remove(this);
			}
		}

		public override int GetHashCode()
		{
			return Gen.HashCombineInt(ID, 1538478890);
		}
	}
}
