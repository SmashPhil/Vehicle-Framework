using System;
using System.Collections.Generic;
using Verse;

namespace Vehicles
{
	public delegate bool WaterRegionEntryPredicate(VehicleRegion from, VehicleRegion to);

	public delegate bool WaterRegionProcessor(VehicleRegion reg);
	public static class WaterRegionTraverser
	{
		private static Queue<BFSWorker> freeWorkers = new Queue<BFSWorker>();

		public static int NumWorkers = 8;

		public static readonly WaterRegionEntryPredicate PassAll = (VehicleRegion from, VehicleRegion to) => true;

		static WaterRegionTraverser()
		{
			RecreateWorkers();
		}

		//FloodAndSetRooms

		//FloodAndSetNewRegionIndex

		public static bool WithinRegions(this IntVec3 A, IntVec3 B, Map map, int regionLookCount, TraverseParms traverseParams, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			VehicleRegion region = VehicleGridsUtility.GetRegion(A, map, traversableRegionTypes);
			if (region is null) return false;
			VehicleRegion regB = VehicleGridsUtility.GetRegion(B, map, traversableRegionTypes);
			if (regB is null) return false;
			if (region == regB) return true;
			bool entryCondition(VehicleRegion from, VehicleRegion r) => r.Allows(traverseParams, false);
			bool found = false;
			bool regionProcessor(VehicleRegion r)
			{
				if (r == regB)
				{
					found = true;
					return true;
				}
				return false;
			}
			BreadthFirstTraverse(region, entryCondition, regionProcessor, regionLookCount, traversableRegionTypes);
			return found;
		}

		public static void MarkRegionsBFS(VehicleRegion root, WaterRegionEntryPredicate entryCondition, int maxRegions, int inRadiusMark, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			BreadthFirstTraverse(root, entryCondition, delegate (VehicleRegion r)
			{
				r.mark = inRadiusMark;
				return false;
			}, maxRegions, traversableRegionTypes);
		}

		public static bool ShouldCountRegion(VehicleRegion r)
		{
			return !r.IsDoorway;
		}

		public static void RecreateWorkers()
		{
			freeWorkers.Clear();
			for (int i = 0; i < NumWorkers; i++)
			{
				freeWorkers.Enqueue(new BFSWorker(i));
			}
		}

		public static void BreadthFirstTraverse(IntVec3 start, Map map, WaterRegionEntryPredicate entryCondition, WaterRegionProcessor regionProcessor, int maxRegions = 999999, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			VehicleRegion region = VehicleGridsUtility.GetRegion(start, map, traversableRegionTypes);
			if (region is null) return;
			BreadthFirstTraverse(region, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
		}

		public static void BreadthFirstTraverse(VehicleRegion root, WaterRegionEntryPredicate entryCondition, WaterRegionProcessor regionProcessor, int maxRegions = 999999, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			if (freeWorkers.Count == 0)
			{
				Log.Error("No free workers for BFS. Either BFS recurred deeper than " + NumWorkers + ", or a bug has put this system in an inconsistent state. Resetting.");
				return;
			}
			if(root is null)
			{
				Log.Error("BFS with null root region.");
				return;
			}
			BFSWorker bfsworker = freeWorkers.Dequeue();
			try
			{
				bfsworker.BreadthFirstTraverseWork(root, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
			}
			catch(Exception ex)
			{
				Log.Error("Exception in BreadthFirstTraverse: " + ex.ToString());
			}
			finally
			{
				bfsworker.Clear();
				freeWorkers.Enqueue(bfsworker);
			}
		}

		public static VehicleRoom FloodAndSetRooms(VehicleRegion root, Map map, VehicleRoom existingRoom)
		{
			VehicleRoom floodingRoom;
			if (existingRoom == null)
			{
				floodingRoom = VehicleRoom.MakeNew(map);
			}
			else
			{
				floodingRoom = existingRoom;
			}
			root.Room = floodingRoom;
			if (!root.type.AllowsMultipleRegionsPerRoom())
			{
				return floodingRoom;
			}
			bool entryCondition(VehicleRegion from, VehicleRegion r) => r.type == root.type && r.Room != floodingRoom;
			bool regionProcessor(VehicleRegion r)
			{
				r.Room = floodingRoom;
				return false;
			}
			BreadthFirstTraverse(root, entryCondition, regionProcessor, 999999, RegionType.Set_All);
			return floodingRoom;
		}

		public static void FloodAndSetNewRegionIndex(VehicleRegion root, int newRegionGroupIndex)
		{
			root.newRegionGroupIndex = newRegionGroupIndex;
			if (!root.type.AllowsMultipleRegionsPerRoom())
			{
				return;
			}
			bool entryCondition(VehicleRegion from, VehicleRegion r) => r.type == root.type && r.newRegionGroupIndex < 0;
			bool regionProcessor(VehicleRegion r)
			{
				r.newRegionGroupIndex = newRegionGroupIndex;
				return false;
			}
			BreadthFirstTraverse(root, entryCondition, regionProcessor, 999999, RegionType.Set_All);
		}

		private class BFSWorker
		{
			private Queue<VehicleRegion> open = new Queue<VehicleRegion>();

			private int numRegionsProcessed;
			private uint closedIndex = 1u;
			private int closedArrayPos;

			public BFSWorker(int closedArrayPos)
			{
				this.closedArrayPos = closedArrayPos;
			}

			public void Clear()
			{
				open.Clear();
			}

			private void QueueNewOpenRegion(VehicleRegion region)
			{
				if (region.closedIndex[closedArrayPos] == closedIndex)
				{
					throw new InvalidOperationException("Region is already closed; you can't open it. Region: " + region.ToString());
				}
				open.Enqueue(region);
				region.closedIndex[closedArrayPos] = closedIndex;
			}

			private void FinalizeSearch() { }

			public void BreadthFirstTraverseWork(VehicleRegion root, WaterRegionEntryPredicate entryCondition, WaterRegionProcessor regionProcessor, int maxRegions, RegionType traversableRegionTypes)
			{
				if ((root.type & traversableRegionTypes) == RegionType.None) return;
				closedIndex += 1u;
				open.Clear();
				numRegionsProcessed = 0;
				QueueNewOpenRegion(root);
				while (open.Count > 0)
				{
					VehicleRegion region = open.Dequeue();
					if(VehicleHarmony.debug)
					{
						region.Debug_Notify_Traversed();
					}
					if(!(regionProcessor is null) && regionProcessor(region))
					{
						FinalizeSearch();
						return;
					}
					if (ShouldCountRegion(region))
					{
						numRegionsProcessed++;
					}
					if(numRegionsProcessed >= maxRegions)
					{
						FinalizeSearch();
						return;
					}
					for(int i = 0; i < region.links.Count; i++)
					{
						VehicleRegionLink regionLink = region.links[i];
						for(int j = 0; j < 2; j++)
						{
							VehicleRegion region2 = regionLink.regions[j];
							if(!(region2 is null) && region2.closedIndex[closedArrayPos] != closedIndex && (region2.type & traversableRegionTypes) != RegionType.None &&
								(entryCondition is null || entryCondition(region, region2)))
							{
								QueueNewOpenRegion(region2);
							}
						}
					}
				}
				FinalizeSearch();
			}
		}
	}
}
