using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vehicles.AI;
using Verse;

namespace Vehicles
{
    public class WaterRegionAndRoomUpdater
    {
        public WaterRegionAndRoomUpdater(Map map)
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
                this.enabledInt = value;
            }
        }

        public bool AnythingToRebuild => !initialized;

        public void RebuildAllWaterRegions()
        {
            if (!Enabled)
                Log.Warning("Called RebuildAllRegions but WaterRegionAndRoomUpdater is disabled. Regions won't be rebuilt.", false);
            map.GetComponent<WaterMap>().getWaterRegionDirtyer.SetAllDirty();
            TryRebuildWaterRegions();
        }

        public void TryRebuildWaterRegions()
        {
            if (working || !Enabled)
                return;
            working = true;
            if (!initialized)
                RebuildAllWaterRegions();
            if(!map.GetComponent<WaterMap>().getWaterRegionDirtyer.AnyDirty)
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
                Log.Error("Exception while rebuilding water regions: " + exc, false);
            }
            newRegions.Clear();
            map.GetComponent<WaterMap>().getWaterRegionDirtyer.SetAllClean();
            initialized = true;
            working = false;
        }

        private void RegenerateNewWaterRegions()
        {
            newRegions.Clear();
            List<IntVec3> cells = map.GetComponent<WaterMap>().getWaterRegionDirtyer.DirtyCells;

            foreach(IntVec3 c  in cells)
            {
                if(WaterGridsUtility.GetRegion(c, map, RegionType.Set_All) is null)
                {
                    WaterRegion region = map.GetComponent<WaterMap>().getWaterRegionmaker.TryGenerateRegionFrom(c);
                    
                    if (!(region is null))
                        newRegions.Add(region);
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
			foreach (WaterRoom room in reusedOldRooms)
			{
				room.newOrReusedRoomGroupIndex = -1;
			}
			foreach (WaterRoom room2 in reusedOldRooms.Concat(newRooms))
			{
				if (room2.newOrReusedRoomGroupIndex < 0)
				{
					tmpRoomStack.Clear();
					tmpRoomStack.Push(room2);
					room2.newOrReusedRoomGroupIndex = num;
					while (tmpRoomStack.Count != 0)
					{
						WaterRoom room3 = tmpRoomStack.Pop();
						foreach (WaterRoom room4 in room3.Neighbors)
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

		private bool ShouldBeInTheSameRoomGroup(WaterRoom a, WaterRoom b)
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
					if (this.currentRegionGroup.Count != 1)
					{
						Log.Error("Region type doesn't allow multiple regions per room but there are >1 regions in this group.", false);
					}
					WaterRoom room = WaterRoom.MakeNew(map);
					currentRegionGroup[0].Room = room;
					newRooms.Add(room);
				}
				else
				{
					bool flag;
					WaterRoom room2 = FindCurrentRegionGroupNeighborWithMostRegions(out flag);
					if (room2 == null)
					{
						WaterRoom item = WaterRegionTraverser.FloodAndSetRooms(currentRegionGroup[0], map, null);
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
			for (int i = 0; i < this.newRegions.Count; i++)
			{
				if (this.newRegions[i].newRegionGroupIndex < 0)
				{
					WaterRegionTraverser.FloodAndSetNewRegionIndex(newRegions[i], num);
					num++;
				}
			}
			return num;
		}

        private WaterRoom FindCurrentRegionGroupNeighborWithMostRegions(out bool multipleOldNeighborRooms)
		{
			multipleOldNeighborRooms = false;
			WaterRoom room = null;
			for (int i = 0; i < currentRegionGroup.Count; i++)
			{
				foreach (WaterRegion region in currentRegionGroup[i].NeighborsOfSameType)
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




        private Map map;

        private List<WaterRegion> newRegions = new List<WaterRegion>();

        private List<WaterRoom> newRooms = new List<WaterRoom>();

        private HashSet<WaterRoom> reusedOldRooms = new HashSet<WaterRoom>();

        private List<WaterRegion> currentRegionGroup = new List<WaterRegion>();

        private List<WaterRoom> currentRoomGroup = new List<WaterRoom>();

        private Stack<WaterRoom> tmpRoomStack = new Stack<WaterRoom>();

        private bool initialized;

        private bool working;

        private bool enabledInt = true;
    }
}
