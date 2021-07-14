using System;
using System.Collections.Generic;
using Verse;

namespace Vehicles
{
	/// <summary>
	/// Traverser utility methods for traversing between 2 regions
	/// </summary>
	public static class VehicleRegionTraverser
	{
		public delegate bool VehicleRegionEntry(VehicleRegion from, VehicleRegion to);
		public delegate bool VehicleRegionProcessor(VehicleRegion reg);

		private static Queue<BFSWorker> freeWorkers = new Queue<BFSWorker>();

		public static int NumWorkers = 8;

		public static readonly VehicleRegionEntry PassAll = (VehicleRegion from, VehicleRegion to) => true;

		static VehicleRegionTraverser()
		{
			RecreateWorkers();
		}

		/// <summary>
		/// <paramref name="A"/> and <paramref name="B"/> are contained within the same region or can traverse between regions
		/// </summary>
		/// <param name="A"></param>
		/// <param name="B"></param>
		/// <param name="map"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="regionLookCount"></param>
		/// <param name="traverseParams"></param>
		/// <param name="traversableRegionTypes"></param>
		public static bool WithinRegions(this IntVec3 A, IntVec3 B, Map map, VehicleDef vehicleDef, int regionLookCount, TraverseParms traverseParams, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			VehicleRegion regionA = VehicleGridsUtility.GetRegion(A, map, vehicleDef, traversableRegionTypes);
			if (regionA is null)
			{
				return false;
			}
			VehicleRegion regionB = VehicleGridsUtility.GetRegion(B, map, vehicleDef, traversableRegionTypes);
			if (regionB is null)
			{
				return false;
			}
			if (regionA == regionB)
			{
				return true;
			}
			bool entryCondition(VehicleRegion from, VehicleRegion to) => to.Allows(traverseParams, false);
			bool found = false;
			bool regionProcessor(VehicleRegion region)
			{
				if (region == regionB)
				{
					found = true;
					return true;
				}
				return false;
			}
			BreadthFirstTraverse(regionA, entryCondition, regionProcessor, regionLookCount, traversableRegionTypes);
			return found;
		}

		/// <summary>
		/// Perform BFS with <paramref name="region"/>
		/// </summary>
		/// <param name="region"></param>
		/// <param name="entryCondition"></param>
		/// <param name="maxRegions"></param>
		/// <param name="inRadiusMark"></param>
		/// <param name="traversableRegionTypes"></param>
		public static void MarkRegionsBFS(VehicleRegion region, VehicleRegionEntry entryCondition, int maxRegions, int inRadiusMark, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			BreadthFirstTraverse(region, entryCondition, delegate (VehicleRegion r)
			{
				r.mark = inRadiusMark;
				return false;
			}, maxRegions, traversableRegionTypes);
		}

		/// <summary>
		/// Region is a doorway
		/// </summary>
		/// <param name="region"></param>
		public static bool ShouldCountRegion(VehicleRegion region)
		{
			return !region.IsDoorway;
		}

		/// <summary>
		/// Requeue <see cref="BFSWorker"/> workers
		/// </summary>
		public static void RecreateWorkers()
		{
			freeWorkers.Clear();
			for (int i = 0; i < NumWorkers; i++)
			{
				freeWorkers.Enqueue(new BFSWorker(i));
			}
		}

		/// <summary>
		/// BreadthFirstSearch from <paramref name="start"/> and <paramref name="regionProcessor"/>
		/// </summary>
		/// <param name="start"></param>
		/// <param name="map"></param>
		/// <param name="vehicleDef"></param>
		/// <param name="entryCondition"></param>
		/// <param name="regionProcessor"></param>
		/// <param name="maxRegions"></param>
		/// <param name="traversableRegionTypes"></param>
		public static void BreadthFirstTraverse(IntVec3 start, Map map, VehicleDef vehicleDef, VehicleRegionEntry entryCondition, VehicleRegionProcessor regionProcessor, int maxRegions = 999999, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			VehicleRegion region = VehicleGridsUtility.GetRegion(start, map, vehicleDef, traversableRegionTypes);
			if (region is null) return;
			BreadthFirstTraverse(region, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
		}

		/// <summary>
		/// BreadthFirstSearch from <paramref name="root"/> and <paramref name="regionProcessor"/>
		/// </summary>
		/// <param name="root"></param>
		/// <param name="entryCondition"></param>
		/// <param name="regionProcessor"></param>
		/// <param name="maxRegions"></param>
		/// <param name="traversableRegionTypes"></param>
		public static void BreadthFirstTraverse(VehicleRegion root, VehicleRegionEntry entryCondition, VehicleRegionProcessor regionProcessor, int maxRegions = 999999, RegionType traversableRegionTypes = RegionType.Set_Passable)
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

		/// <summary>
		/// Breadth First Search to fill room based on <paramref name="region"/>
		/// </summary>
		/// <param name="root"></param>
		/// <param name="map"></param>
		/// <param name="existingRoom"></param>
		public static VehicleRoom FloodAndSetRooms(VehicleRegion region, Map map, VehicleDef vehicleDef, VehicleRoom existingRoom)
		{
			VehicleRoom floodingRoom;
			if (existingRoom == null)
			{
				floodingRoom = VehicleRoom.MakeNew(map, vehicleDef);
			}
			else
			{
				floodingRoom = existingRoom;
			}
			region.Room = floodingRoom;
			if (!region.type.AllowsMultipleRegionsPerDistrict())
			{
				return floodingRoom;
			}
			bool entryCondition(VehicleRegion from, VehicleRegion r) => r.type == region.type && r.Room != floodingRoom;
			bool regionProcessor(VehicleRegion r)
			{
				r.Room = floodingRoom;
				return false;
			}
			BreadthFirstTraverse(region, entryCondition, regionProcessor, 999999, RegionType.Set_All);
			return floodingRoom;
		}

		/// <summary>
		/// Breadth First Search to assign new region group indices for <paramref name="root"/>
		/// </summary>
		/// <param name="root"></param>
		/// <param name="newRegionGroupIndex"></param>
		public static void FloodAndSetNewRegionIndex(VehicleRegion root, int newRegionGroupIndex)
		{
			root.newRegionGroupIndex = newRegionGroupIndex;
			if (!root.type.AllowsMultipleRegionsPerDistrict())
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

		/// <summary>
		/// Breadth First Search worker class
		/// </summary>
		private class BFSWorker
		{
			private readonly Queue<VehicleRegion> open = new Queue<VehicleRegion>();

			private int numRegionsProcessed;
			private uint closedIndex = 1u;
			private readonly int closedArrayPos;

			public BFSWorker(int closedArrayPos)
			{
				this.closedArrayPos = closedArrayPos;
			}

			/// <summary>
			/// Clear region queue
			/// </summary>
			public void Clear()
			{
				open.Clear();
			}

			/// <summary>
			/// Queue region available for traversal
			/// </summary>
			/// <param name="region"></param>
			private void QueueNewOpenRegion(VehicleRegion region)
			{
				if (region.closedIndex[closedArrayPos] == closedIndex)
				{
					throw new InvalidOperationException($"Region is already closed; you can't open it. Region={region}");
				}
				open.Enqueue(region);
				region.closedIndex[closedArrayPos] = closedIndex;
			}

			/// <summary>
			/// Breadth First Traversal search algorithm
			/// </summary>
			/// <param name="root"></param>
			/// <param name="entryCondition"></param>
			/// <param name="regionProcessor"></param>
			/// <param name="maxRegions"></param>
			/// <param name="traversableRegionTypes"></param>
			public void BreadthFirstTraverseWork(VehicleRegion root, VehicleRegionEntry entryCondition, VehicleRegionProcessor regionProcessor, int maxRegions, RegionType traversableRegionTypes)
			{
				if ((root.type & traversableRegionTypes) == RegionType.None) return;
				closedIndex += 1u;
				open.Clear();
				numRegionsProcessed = 0;
				QueueNewOpenRegion(root);
				while (open.Count > 0)
				{
					VehicleRegion region = open.Dequeue();
					if (VehicleHarmony.debug)
					{
						region.Debug_Notify_Traversed();
					}
					if (regionProcessor != null && regionProcessor(region))
					{
						return;
					}
					if (ShouldCountRegion(region))
					{
						numRegionsProcessed++;
					}
					if (numRegionsProcessed >= maxRegions)
					{
						return;
					}
					for (int i = 0; i < region.links.Count; i++)
					{
						VehicleRegionLink regionLink = region.links[i];
						for (int j = 0; j < 2; j++)
						{
							VehicleRegion region2 = regionLink.regions[j];
							if (region2 != null && region2.closedIndex[closedArrayPos] != closedIndex && (region2.type & traversableRegionTypes) != RegionType.None &&
								(entryCondition is null || entryCondition(region, region2)))
							{
								QueueNewOpenRegion(region2);
							}
						}
					}
				}
			}
		}
	}
}
