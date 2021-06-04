using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public class VehicleRegionAndRoomUpdater
	{
		private Map map;

		private List<VehicleRegion> newRegions = new List<VehicleRegion>();

		private List<VehicleRoom> newRooms = new List<VehicleRoom>();

		private HashSet<VehicleRoom> reusedOldRooms = new HashSet<VehicleRoom>();

		private List<VehicleRegion> currentRegionGroup = new List<VehicleRegion>();

		private List<VehicleRoom> currentRoomGroup = new List<VehicleRoom>();

		private Stack<VehicleRoom> tmpRoomStack = new Stack<VehicleRoom>();

		private bool initialized;

		private bool working;

		private bool enabledInt = true;

		public VehicleRegionAndRoomUpdater(Map map)
		{
			this.map = map;
		}

		public bool Enabled
		{
			get
			{
				return enabledInt;
			}
			set
			{
				enabledInt = value;
			}
		}

		public bool AnythingToRebuild => !initialized;

		public void RebuildAllWaterRegions()
		{
			if (!Enabled)
			{
				Log.Warning("Called RebuildAllRegions but VehicleRegionAndRoomUpdater is disabled. Regions won't be rebuilt.");
			}
			map.GetCachedMapComponent<VehicleMapping>().VehicleRegionDirtyer.SetAllDirty();
			TryRebuildWaterRegions();
		}

		public void TryRebuildWaterRegions()
		{
			if (working || !Enabled)
			{
				return;
			}
			working = true;
			if (!initialized)
			{
				RebuildAllWaterRegions();
			}
			if(!map.GetCachedMapComponent<VehicleMapping>().VehicleRegionDirtyer.AnyDirty)
			{
				working = false;
				return;
			}
			try
			{
				RegenerateNewWaterRegions();
				CreateOrUpdateWaterRooms();
			}
			catch(Exception exc)
			{
				Log.Error("Exception while rebuilding water regions: " + exc);
			}
			newRegions.Clear();
			map.GetCachedMapComponent<VehicleMapping>().VehicleRegionDirtyer.SetAllClean();
			initialized = true;
			working = false;
		}

		private void RegenerateNewWaterRegions()
		{
			newRegions.Clear();
			List<IntVec3> cells = map.GetCachedMapComponent<VehicleMapping>().VehicleRegionDirtyer.DirtyCells;
			foreach (IntVec3 c  in cells)
			{
				if (VehicleGridsUtility.GetRegion(c, map, RegionType.Set_All) is null)
				{
					VehicleRegion region = map.GetCachedMapComponent<VehicleMapping>().VehicleRegionMaker.TryGenerateRegionFrom(c);

					if (!(region is null))
					{
						newRegions.Add(region);
					}
				}
			}
		}

		private void CreateOrUpdateWaterRooms()
		{
			newRooms.Clear();
			reusedOldRooms.Clear();
			//newRoomGroups.Clear();
			//reusedOldRoomGroups.Clear();
			int numRegionGroups = CombineNewRegionsIntoContiguousGroups();
			CreateOrAttachToExistingRooms(numRegionGroups);
			int numRoomGroups = CombineNewAndReusedRoomsIntoContiguousGroups();
			//CreateOrAttachToExistingRoomGroups(numRoomGroups);
			//NotifyAffectedRoomsAndRoomGroupsAndUpdateTemperature();
			newRooms.Clear();
			reusedOldRooms.Clear();
			//newRoomGroups.Clear();
			//reusedOldRoomGroups.Clear();
		}

		private int CombineNewAndReusedRoomsIntoContiguousGroups()
		{
			int num = 0;
			foreach (VehicleRoom room in reusedOldRooms)
			{
				room.newOrReusedRoomGroupIndex = -1;
			}
			foreach (VehicleRoom room2 in reusedOldRooms.Concat(newRooms))
			{
				if (room2.newOrReusedRoomGroupIndex < 0)
				{
					tmpRoomStack.Clear();
					tmpRoomStack.Push(room2);
					room2.newOrReusedRoomGroupIndex = num;
					while (tmpRoomStack.Count != 0)
					{
						VehicleRoom room3 = tmpRoomStack.Pop();
						foreach (VehicleRoom room4 in room3.Neighbors)
						{
							if (room4.newOrReusedRoomGroupIndex < 0 && ShouldBeInTheSameRoomGroup(room3, room4))
							{
								room4.newOrReusedRoomGroupIndex = num;
								tmpRoomStack.Push(room4);
							}
						}
					}
					tmpRoomStack.Clear();
					num++;
				}
			}
			return num;
		}

		private bool ShouldBeInTheSameRoomGroup(VehicleRoom a, VehicleRoom b)
		{
			RegionType regionType = a.RegionType;
			RegionType regionType2 = b.RegionType;
			return (regionType == RegionType.Normal || regionType == RegionType.ImpassableFreeAirExchange) && (regionType2 == RegionType.Normal || regionType2 == RegionType.ImpassableFreeAirExchange);
		}

		private void CreateOrAttachToExistingRooms(int numRegionGroups)
		{
			for (int i = 0; i < numRegionGroups; i++)
			{
				currentRegionGroup.Clear();
				for (int j = 0; j < newRegions.Count; j++)
				{
					if (newRegions[j].newRegionGroupIndex == i)
					{
						currentRegionGroup.Add(newRegions[j]);
					}
				}
				if (!currentRegionGroup[0].type.AllowsMultipleRegionsPerRoom())
				{
					if (currentRegionGroup.Count != 1)
					{
						Log.Error("Region type doesn't allow multiple regions per room but there are >1 regions in this group.");
					}
					VehicleRoom room = VehicleRoom.MakeNew(map);
					currentRegionGroup[0].Room = room;
					newRooms.Add(room);
				}
				else
				{
					VehicleRoom room2 = FindCurrentRegionGroupNeighborWithMostRegions(out bool flag);
					if (room2 is null)
					{
						VehicleRoom item = WaterRegionTraverser.FloodAndSetRooms(currentRegionGroup[0], map, null);
						newRooms.Add(item);
					}
					else if (!flag)
					{
						for (int k = 0; k < currentRegionGroup.Count; k++)
						{
							currentRegionGroup[k].Room = room2;
						}
						reusedOldRooms.Add(room2);
					}
					else
					{
						WaterRegionTraverser.FloodAndSetRooms(currentRegionGroup[0], map, room2);
						reusedOldRooms.Add(room2);
					}
				}
			}
		}

		private int CombineNewRegionsIntoContiguousGroups()
		{
			int num = 0;
			for (int i = 0; i < newRegions.Count; i++)
			{
				if (newRegions[i].newRegionGroupIndex < 0)
				{
					WaterRegionTraverser.FloodAndSetNewRegionIndex(newRegions[i], num);
					num++;
				}
			}
			return num;
		}

		private VehicleRoom FindCurrentRegionGroupNeighborWithMostRegions(out bool multipleOldNeighborRooms)
		{
			multipleOldNeighborRooms = false;
			VehicleRoom room = null;
			for (int i = 0; i < currentRegionGroup.Count; i++)
			{
				foreach (VehicleRegion region in currentRegionGroup[i].NeighborsOfSameType)
				{
					if (region.Room != null && !reusedOldRooms.Contains(region.Room))
					{
						if (room == null)
						{
							room = region.Room;
						}
						else if (region.Room != room)
						{
							multipleOldNeighborRooms = true;
							if (region.Room.RegionCount > room.RegionCount)
							{
								room = region.Room;
							}
						}
					}
				}
			}
			return room;
		}
	}
}
