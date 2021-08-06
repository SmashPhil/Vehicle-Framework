using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	/// <summary>
	/// Region and room update handler
	/// </summary>
	public class VehicleRegionAndRoomUpdater
	{
		private readonly Map map;
		private readonly VehicleDef vehicleDef;

		private readonly List<VehicleRegion> newRegions = new List<VehicleRegion>();
		private readonly List<VehicleRoom> newRooms = new List<VehicleRoom>();
		private readonly HashSet<VehicleRoom> reusedOldRooms = new HashSet<VehicleRoom>();

		private readonly List<VehicleRegion> currentRegionGroup = new List<VehicleRegion>();
		private readonly List<VehicleRoom> currentRoomGroup = new List<VehicleRoom>();

		private readonly Stack<VehicleRoom> tmpRoomStack = new Stack<VehicleRoom>();

		public VehicleRegionAndRoomUpdater(Map map, VehicleDef vehicleDef)
		{
			this.map = map;
			this.vehicleDef = vehicleDef;
		}

		/// <summary>
		/// Updater has been initialized
		/// </summary>
		public bool Initialized { get; private set; }

		/// <summary>
		/// Currently updating regions
		/// </summary>
		public bool UpdatingRegion { get; private set; }

		/// <summary>
		/// Anything in RegionGrid that needs to be rebuilt
		/// </summary>
		public bool AnythingToRebuild => !Initialized;

		/// <summary>
		/// Updater has finished initial build
		/// </summary>
		public bool Enabled { get; internal set; }

		/// <summary>
		/// Rebuild all regions
		/// </summary>
		public void RebuildAllVehicleRegions()
		{
			if (!Enabled)
			{
				Log.Warning("Called RebuildAllRegions but VehicleRegionAndRoomUpdater is disabled. Regions won't be rebuilt.");
			}
			map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionDirtyer.SetAllDirty();
			TryRebuildVehicleRegions();
		}

		/// <summary>
		/// Rebuild all regions on the map and generate associated rooms
		/// </summary>
		public void TryRebuildVehicleRegions()
		{
			if (UpdatingRegion || !Enabled)
			{
				return;
			}
			string updateStep = "Initializing";
			UpdatingRegion = true;
			if (!Initialized)
			{
				RebuildAllVehicleRegions();
			}
			if (!map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionDirtyer.AnyDirty)
			{
				UpdatingRegion = false;
				return;
			}
			try
			{
				updateStep = "Generating new regions";
				RegenerateNewVehicleRegions();
				updateStep = "Creating or updating rooms";
				CreateOrUpdateVehicleRooms();
			}
			catch(Exception ex)
			{
				Log.Error($"Exception while rebuilding vehicle regions for {vehicleDef}. Last step: {updateStep} Exception={ex.Message}");
			}
			newRegions.Clear();
			map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionDirtyer.SetAllClean();
			Initialized = true;
			UpdatingRegion = false;
		}

		/// <summary>
		/// Generate regions with dirty cells
		/// </summary>
		private void RegenerateNewVehicleRegions()
		{
			newRegions.Clear();
			HashSet<IntVec3> cells = map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionDirtyer.DirtyCells;
			foreach (IntVec3 cell in cells)
			{
				if (VehicleGridsUtility.GetRegion(cell, map, vehicleDef, RegionType.Set_All) is null)
				{
					VehicleRegion region = map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionMaker.TryGenerateRegionFrom(cell);
					if (region != null)
					{
						newRegions.Add(region);
					}
				}
			}
		}

		/// <summary>
		/// Update procedure for Rooms associated with Vehicle based regions
		/// </summary>
		private void CreateOrUpdateVehicleRooms()
		{
			newRooms.Clear();
			reusedOldRooms.Clear();
			int numRegionGroups = CombineNewRegionsIntoContiguousGroups();
			CreateOrAttachToExistingRooms(numRegionGroups);
			CombineNewAndReusedRoomsIntoContiguousGroups();
			newRooms.Clear();
			reusedOldRooms.Clear();
		}

		/// <summary>
		/// Combine rooms together with room group criteria met
		/// </summary>
		private int CombineNewAndReusedRoomsIntoContiguousGroups()
		{
			int num = 0;
			for (int i = 0; i < newRegions.Count; i++)
			{
				if (newRegions[i].newRegionGroupIndex < 0)
				{
					VehicleRegionTraverser.FloodAndSetNewRegionIndex(newRegions[i], num);
					num++;
				}
			}
			return num;
		}

		/// <summary>
		/// Create new room or attach to existing room with predetermined number of region groups
		/// </summary>
		/// <param name="numRegionGroups"></param>
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
				if (!currentRegionGroup[0].type.AllowsMultipleRegionsPerDistrict())
				{
					if (currentRegionGroup.Count != 1)
					{
						Log.Error("Region type doesn't allow multiple regions per room but there are >1 regions in this group.");
					}
					VehicleRoom room = VehicleRoom.MakeNew(map, vehicleDef);
					currentRegionGroup[0].Room = room;
					newRooms.Add(room);
				}
				else
				{
					VehicleRoom room2 = FindCurrentRegionGroupNeighborWithMostRegions(out bool flag);
					if (room2 is null)
					{
						VehicleRoom item = VehicleRegionTraverser.FloodAndSetRooms(currentRegionGroup[0], map, vehicleDef, null);
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
						VehicleRegionTraverser.FloodAndSetRooms(currentRegionGroup[0], map, vehicleDef, room2);
						reusedOldRooms.Add(room2);
					}
				}
			}
		}

		/// <summary>
		/// Combine regions that meet region group criteria
		/// </summary>
		private int CombineNewRegionsIntoContiguousGroups()
		{
			int num = 0;
			for (int i = 0; i < newRegions.Count; i++)
			{
				if (newRegions[i].newRegionGroupIndex < 0)
				{
					VehicleRegionTraverser.FloodAndSetNewRegionIndex(newRegions[i], num);
					num++;
				}
			}
			return num;
		}

		/// <summary>
		/// Find neighboring region group with most regions
		/// </summary>
		/// <param name="multipleOldNeighborRooms"></param>
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
