using System;
using System.Collections.Generic;
using SmashTools;
using SmashTools.Performance;
using UnityEngine;
using Verse;

namespace Vehicles
{
	/// <summary>
	/// Region and room update handler
	/// </summary>
	public class VehicleRegionAndRoomUpdater : VehicleRegionManager
    {
		private readonly List<VehicleRegion> newRegions = new List<VehicleRegion>();

		private readonly List<VehicleRoom> newRooms = new List<VehicleRoom>();
		private readonly HashSet<VehicleRoom> reusedOldRooms = new HashSet<VehicleRoom>();

		private readonly List<VehicleRegion> currentRegionGroup = new List<VehicleRegion>();

		public VehicleRegionAndRoomUpdater(VehicleMapping mapping, VehicleDef createdFor) : base(mapping, createdFor)
		{
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

		public void Init()
		{
			Enabled = true;
			mapping[createdFor].Suspended = false;
			mapping[createdFor].VehicleRegionGrid.Init();
		}

		public void Release()
		{
			Initialized = false;
			Enabled = false;
			mapping[createdFor].Suspended = true;
			mapping[createdFor].VehicleRegionGrid.Release();
		}

		/// <summary>
		/// Should only be called for map generation so spawn events don't attempt to rebuild regions. 
		/// </summary>
		/// <remarks>There is no way to re-enable regions outside of calling RebuildAll, which does occur post map-generation.</remarks>
		public void Disable()
		{
			Enabled = false;
		}

		/// <summary>
		/// Rebuild all regions
		/// </summary>
		public void RebuildAllVehicleRegions()
		{
			if (!Enabled)
			{
				Log.Warning($"Called RebuildAllVehicleRegions but VehicleRegionAndRoomUpdater is disabled. VehicleRegions won't be rebuilt. StackTrace: {StackTraceUtility.ExtractStackTrace()}");
			}
			mapping[createdFor].VehicleRegionDirtyer.SetAllDirty();
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

			UpdatingRegion = true;
			if (!Initialized)
			{
				RebuildAllVehicleRegions();
			}
			if (!mapping[createdFor].VehicleRegionDirtyer.AnyDirty)
			{
				UpdatingRegion = false;
				return;
			}
			try
			{
				RegenerateNewVehicleRegions();
				CreateOrUpdateVehicleRooms();
			}
			catch (Exception ex)
			{
				Log.Error($"Exception while rebuilding vehicle regions for {createdFor}. Exception={ex}");
			}
			newRegions.Clear();
			Initialized = true;
			UpdatingRegion = false;
		}

		/// <summary>
		/// Generate regions with dirty cells
		/// </summary>
		private void RegenerateNewVehicleRegions()
		{
			newRegions.Clear();
			VehicleMapping.VehiclePathData pathData = mapping[createdFor];
			foreach (IntVec3 cell in pathData.VehicleRegionDirtyer.DirtyCells)
			{
				if (!cell.InBounds(mapping.map))
				{
					continue;
				}
				VehicleRegion region = pathData.VehicleRegionGrid.GetRegionAt(cell);
				bool needsNew = region == null || !region.valid;

				// Buffer should never hold a region which still has references in the region grid.
				Assert.IsTrue(region == null || !region.Suspended, $"{region} has been pushed to buffer prematurely.");

				if (needsNew && pathData.VehicleRegionMaker.TryGenerateRegionFrom(cell, out region))
				{
					newRegions.Add(region);
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
					VehicleRoom room = VehicleRoom.MakeNew(mapping.map, createdFor);
					currentRegionGroup[0].Room = room;
					newRooms.Add(room);
				}
				else
				{
					VehicleRoom room2 = FindCurrentRegionGroupNeighborWithMostRegions(out bool flag);
					if (room2 is null)
					{
						VehicleRoom item = VehicleRegionTraverser.FloodAndSetRooms(currentRegionGroup[0], mapping.map, createdFor, null);
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
						VehicleRegionTraverser.FloodAndSetRooms(currentRegionGroup[0], mapping.map, createdFor, room2);
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
