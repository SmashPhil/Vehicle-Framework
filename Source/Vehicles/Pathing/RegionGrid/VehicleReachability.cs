using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using SmashTools;
using SmashTools.Pathfinding;

namespace Vehicles
{
	/// <summary>
	/// Reachability calculator for quick result path finding before running the algorithm
	/// </summary>
	public sealed class VehicleReachability
	{
		private readonly VehicleMapping mapping;
		private readonly VehicleDef createdFor;

		private readonly Queue<VehicleRegion> openQueue = new Queue<VehicleRegion>();
		private readonly FastPriorityQueue<VehicleRegion> chunkQueue = new FastPriorityQueue<VehicleRegion>();

		private readonly List<VehicleRegion> startingRegions = new List<VehicleRegion>();
		private readonly List<VehicleRegion> destRegions = new List<VehicleRegion>();

		private uint reachedIndex = 1;

		private VehicleReachabilityCache cache = new VehicleReachabilityCache();

		private VehiclePathGrid pathGrid;
		private VehicleRegionGrid regionGrid;

		public VehicleReachability(VehicleMapping mapping, VehicleDef createdFor)
		{
			this.mapping = mapping;
			this.createdFor = createdFor;
		}

		/// <summary>
		/// Currently calculating reachability between regions
		/// </summary>
		private bool CalculatingReachability { get; set; }

		public VehiclePathGrid PathGrid
		{
			get
			{
				if (pathGrid is null)
				{
					//Pathgrid is strictly for impassable cost check, so this will still match for copies
					pathGrid = mapping[createdFor].VehiclePathGrid;
				}
				return pathGrid;
			}
		}

		public VehicleRegionGrid RegionGrid
		{
			get
			{
				if (regionGrid is null)
				{
					regionGrid = mapping[createdFor].VehicleRegionGrid;
				}
				return regionGrid;
			}
		}

		/// <summary>
		/// Clear reachability cache
		/// </summary>
		public void ClearCache()
		{
			if (cache.Count > 0)
			{
				cache.Clear();
			}
		}

		/// <summary>
		/// Clear reachability cache for specific vehicle
		/// </summary>
		/// <param name="vehicle"></param>
		public void ClearCacheFor(VehiclePawn vehicle)
		{
			cache.ClearFor(vehicle);
		}

		/// <summary>
		/// Clear reachability cache for targets retaining hostile Pawn
		/// </summary>
		/// <param name="hostileTo"></param>
		public void ClearCacheForHostile(Thing hostileTo)
		{
			cache.ClearForHostile(hostileTo);
		}

		/// <summary>
		/// Queue region for reachability check
		/// </summary>
		/// <param name="region"></param>
		private void QueueNewOpenRegion(VehicleRegion region)
		{
			if (region is null)
			{
				Log.ErrorOnce("Tried to queue null region (Vehicles).", "NullVehicleRegion".GetHashCode());
				return;
			}
			if (region.reachedIndex == reachedIndex)
			{
				Log.ErrorOnce($"VehicleRegion is already reached; you can't open it. VehicleRegion={region}", region.GetHashCode());
				return;
			}
			openQueue.Enqueue(region);
			region.reachedIndex = reachedIndex; //Ensures region links don't back traverse to regions already visited
		}

		private void QueueChunk(VehicleRegion region)
		{
			if (region is null)
			{
				Log.ErrorOnce($"[{VehicleHarmony.LogLabel}] Tried to queue null region.", "NullVehicleRegion".GetHashCode());
				return;
			}
			if (region.reachedIndex == reachedIndex)
			{
				Log.ErrorOnce($"[{VehicleHarmony.LogLabel}] VehicleRegion has already been reached, attempting to retrace which may result in infinite loops. VehicleRegion={region}", region.GetHashCode());
				return;
			}
			openQueue.Enqueue(region);
			region.reachedIndex = reachedIndex;
		}

		/// <summary>
		/// <seealso cref="CanReachVehicle(IntVec3, LocalTargetInfo, PathEndMode, TraverseParms)"/>
		/// </summary>
		/// <param name="start"></param>
		/// <param name="dest"></param>
		/// <param name="peMode"></param>
		/// <param name="traverseMode"></param>
		/// <param name="maxDanger"></param>
		public bool CanReachVehicleNonLocal(IntVec3 start, TargetInfo dest, PathEndMode peMode, TraverseMode traverseMode, Danger maxDanger)
		{
			return (dest.Map is null || dest.Map == mapping.map) && CanReachVehicle(start, (LocalTargetInfo)dest, peMode, traverseMode, maxDanger);
		}

		/// <summary>
		/// <seealso cref="CanReachVehicle(IntVec3, LocalTargetInfo, PathEndMode, TraverseParms)"/>
		/// </summary>
		/// <param name="start"></param>
		/// <param name="dest"></param>
		/// <param name="peMode"></param>
		/// <param name="traverseParms"></param>
		public bool CanReachVehicleNonLocal(IntVec3 start, TargetInfo dest, PathEndMode peMode, TraverseParms traverseParms)
		{
			return (dest.Map is null || dest.Map == mapping.map) && CanReachVehicle(start, (LocalTargetInfo)dest, peMode, traverseParms);
		}

		/// <summary>
		/// <seealso cref="CanReachVehicle(IntVec3, LocalTargetInfo, PathEndMode, TraverseParms)"/>
		/// </summary>
		/// <param name="start"></param>
		/// <param name="dest"></param>
		/// <param name="peMode"></param>
		/// <param name="traverseMode"></param>
		/// <param name="maxDanger"></param>
		public bool CanReachVehicle(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseMode traverseMode, Danger maxDanger)
		{
			return CanReachVehicle(start, dest, peMode, TraverseParms.For(traverseMode, maxDanger, false));
		}

		/// <summary>
		/// Traverse by cell or by region to determine reachability for Vehicle
		/// </summary>
		/// <param name="start"></param>
		/// <param name="dest"></param>
		/// <param name="peMode"></param>
		/// <param name="traverseParms"></param>
		/// <returns>start can reach destination target</returns>
		public bool CanReachVehicle(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParms)
		{
			if (!ValidateCanStart(start, dest, traverseParms, out VehicleDef vehicleDef))
			{
				return false;
			}

			if (!PathGrid.WalkableFast(start))
			{
				return false;
			}
			bool freeTraversal = traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater && traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater;
			if ((peMode == PathEndMode.OnCell || peMode == PathEndMode.Touch || peMode == PathEndMode.ClosestTouch) && freeTraversal)
			{
				VehicleRoom room = VehicleRegionAndRoomQuery.RoomAtFast(start, mapping.map, createdFor, RegionType.Set_Passable);
				if (room != null && room == VehicleRegionAndRoomQuery.RoomAtFast(dest.Cell, mapping.map, createdFor, RegionType.Set_Passable))
				{
					return true;
				}
			}

			if (traverseParms.mode == TraverseMode.PassAllDestroyableThings)
			{
				TraverseParms traverseParms2 = traverseParms;
				traverseParms.mode = TraverseMode.PassDoors;
				if (CanReachVehicle(start, dest, peMode, traverseParms2))
				{
					return true;
				}
			}

			//Try to use parms vehicle if possible for pathgrid check
			dest = (LocalTargetInfo)GenPathVehicles.ResolvePathMode(vehicleDef, mapping.map, dest.ToTargetInfo(mapping.map), ref peMode);
			CalculatingReachability = true;
			bool result;
			try
			{
				reachedIndex += 1;
				destRegions.Clear();
				if (peMode == PathEndMode.OnCell)
				{
					VehicleRegion region = VehicleGridsUtility.GetRegion(dest.Cell, mapping.map, createdFor, RegionType.Set_Passable);
					if (region != null && region.Allows(traverseParms, true))
					{
						destRegions.Add(region);
					}
				}
				else if (peMode == PathEndMode.Touch)
				{
					TouchPathEndModeUtilityVehicles.AddAllowedAdjacentRegions(dest, traverseParms, mapping.map, createdFor, destRegions);
				}
				if (destRegions.Count == 0 && traverseParms.mode != TraverseMode.PassAllDestroyableThings && traverseParms.mode !=
					TraverseMode.PassAllDestroyableThingsNotWater)
				{
					result = false;
				}
				else
				{
					destRegions.RemoveDuplicates();
					openQueue.Clear();
					DetermineStartRegions(start);
					if (openQueue.Count == 0 && traverseParms.mode != TraverseMode.PassAllDestroyableThings && traverseParms.mode !=
						TraverseMode.PassAllDestroyableThingsNotWater)
					{
						result = false;
					}
					else
					{
						if (startingRegions.Any() && destRegions.Any() && CanUseCache(traverseParms.mode))
						{
							BoolUnknown cachedResult = GetCachedResult(traverseParms);
							if (cachedResult == BoolUnknown.True)
							{
								return true;
							}
							if (cachedResult == BoolUnknown.False)
							{
								return false;
							}
						}
						if (traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater ||
							traverseParms.mode == TraverseMode.NoPassClosedDoorsOrWater)
						{
							bool flag = CheckCellBasedReachability(start, dest, peMode, traverseParms);
							result = flag;
						}
						else
						{
							bool flag2 = CheckRegionBasedReachability(traverseParms);
							result = flag2;
						}
					}
				}
			}
			finally
			{
				CalculatingReachability = false;
			}
			return result;
		}

		/// <summary>
		/// Determine starting region either on starting cell or cardinal / diagonal to it.
		/// </summary>
		/// <param name="start"></param>
		private void DetermineStartRegions(IntVec3 start)
		{
			startingRegions.Clear();
			if (PathGrid.WalkableFast(start))
			{
				VehicleRegion validRegionAt = RegionGrid.GetValidRegionAt(start);
				QueueNewOpenRegion(validRegionAt);
				startingRegions.Add(validRegionAt);
			}
			else
			{
				for (int i = 0; i < 8; i++)
				{
					IntVec3 c = start + GenAdj.AdjacentCells[i];
					if (c.InBounds(mapping.map))
					{
						if (PathGrid.WalkableFast(c))
						{
							VehicleRegion validRegionAt = RegionGrid.GetValidRegionAt(c);
							if (validRegionAt != null && validRegionAt.reachedIndex != reachedIndex)
							{
								QueueNewOpenRegion(validRegionAt);
								startingRegions.Add(validRegionAt);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Retrieve cached value for reachability result
		/// </summary>
		/// <param name="traverseParms"></param>
		private BoolUnknown GetCachedResult(TraverseParms traverseParms)
		{
			bool flag = false;
			for (int i = 0; i < startingRegions.Count; i++)
			{
				for (int j = 0; j < destRegions.Count; j++)
				{
					if (destRegions[j] == startingRegions[i])
					{
						return BoolUnknown.True;
					}
					BoolUnknown boolUnknown = cache.CachedResultFor(startingRegions[i].Room, destRegions[j].Room, traverseParms);
					if (boolUnknown == BoolUnknown.True)
					{
						return BoolUnknown.True;
					}
					if (boolUnknown == BoolUnknown.Unknown)
					{
						flag = true;
					}
				}
			}
			if (!flag)
			{
				return BoolUnknown.False;
			}
			return BoolUnknown.Unknown;
		}

		/// <summary>
		/// Determine reachability by region traversal
		/// </summary>
		/// <param name="traverseParms"></param>
		private bool CheckRegionBasedReachability(TraverseParms traverseParms)
		{
			while (openQueue.Count > 0)
			{
				VehicleRegion region = openQueue.Dequeue();
				foreach (VehicleRegionLink regionLink in region.links)
				{
					for (int i = 0; i < 2; i++)
					{
						VehicleRegion linkedRegion = regionLink.regions[i];
						if (linkedRegion != null && linkedRegion.reachedIndex != reachedIndex && linkedRegion.type.Passable())
						{
							if (linkedRegion.Allows(traverseParms, false))
							{
								if (destRegions.Contains(linkedRegion))
								{
									foreach (VehicleRegion startRegion in startingRegions)
									{
										cache.AddCachedResult(startRegion.Room, linkedRegion.Room, traverseParms, true);
									}
									return true;
								}
								QueueNewOpenRegion(linkedRegion);
							}
						}
					}
				}
			}
			foreach (VehicleRegion startRegion in startingRegions)
			{
				foreach (VehicleRegion destRegion in destRegions)
				{
					cache.AddCachedResult(startRegion.Room, destRegion.Room, traverseParms, false);
				}
			}
			return false;
		}

		public ChunkList FindChunks(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParms, bool debugDrawSearch = false)
		{
			if (ValidateCanStart(start, dest, traverseParms, out VehicleDef _))
			{
				openQueue.Clear();
				reachedIndex++;

				VehicleRegion startingRegion = RegionGrid.GetValidRegionAt(start);
				if (startingRegion == null)
				{
					Log.Error($"Unable to fetch valid starting region at {start}.");
					return null;
				}
				VehicleRegion destinationRegion = VehicleGridsUtility.GetRegion(dest.Cell, mapping.map, createdFor, RegionType.Set_Passable);
				if (startingRegion == null || !destinationRegion.Allows(traverseParms, true))
				{
					Log.Error($"Unable to fetch valid starting region that allows traverseParms={traverseParms} at {start}.");
					return null;
				}

				if (startingRegion == destinationRegion)
				{
					return null; //no need for HPA* in same region
				}
				return new AStar(mapping, createdFor).Run(startingRegion, destinationRegion, traverseParms, debugDrawSearch: debugDrawSearch);
			}
			Log.Message($"Can't validate for chunk search");
			return null;
		}

		private static void MarkRegionForDrawing(VehicleRegion region, Map map, bool drawLinks = true)
		{
			foreach (IntVec3 cell in region.Cells)
			{
				map.debugDrawer.FlashCell(cell, colorPct: 0.65f, duration: 180);
			}

			if (drawLinks)
			{
				for (int i = 0; i < region.links.Count; i++)
				{
					VehicleRegionLink regionLink = region.links[i];
					foreach (VehicleRegionLink toRegionLink in region.links)
					{
						if (regionLink == toRegionLink) continue;
						float weight = region.WeightBetween(regionLink, toRegionLink).cost;
						regionLink.DrawWeight(toRegionLink, weight);
					}
				}
#if DEBUG
				Thread.Sleep(5);
#endif
			}
		}

		private bool ValidateCanStart(IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, out VehicleDef forVehicleDef)
		{
			if (CalculatingReachability)
			{
				Log.ErrorOnce("Called CanReachVehicle() while working. This should never happen. Suppressing further errors.", "CanReachVehicleWorkingError".GetHashCode());
				forVehicleDef = null;
				return false;
			}
			VehiclePawn vehicle = traverseParms.pawn as VehiclePawn;
			forVehicleDef = vehicle?.VehicleDef ?? createdFor;
			if (vehicle != null)
			{
				if (!vehicle.Spawned)
				{
					Log.Error($"Attempting reachability check for unspawned vehicle {vehicle}.");
					return false;
				}
				if (vehicle.Map != mapping.map)
				{
					Log.Error($"Called CanReach with a vehicle not spawned on this map. This means that we can't check its reachability here. Vehicle's current map should have been used instead. vehicle={vehicle} vehicle.Map={vehicle.Map} map={mapping.map}");
					return false;
				}
			}
			if (!dest.IsValid)
			{
				Debug.Warning($"Destination Invalid.");
				return false;
			}
			if (dest.HasThing && dest.Thing.Map != mapping.map)
			{
				Log.Error($"Called CanReach for regions of a different map than destination.  Destination={dest} Map={mapping.map} Destination.Map={dest.Thing.Map}");
				return false;
			}
			if (!start.InBounds(mapping.map) || !dest.Cell.InBounds(mapping.map))
			{
				Debug.Warning($"Start or Destination out of bounds for reachability check.");
				return false;
			}
			return true;
		}

		/// <summary>
		/// Determine reachability by cell traversal
		/// </summary>
		/// <remarks>
		/// Only use outside normal search conditions, performance varies on distance
		/// </remarks>
		/// <param name="start"></param>
		/// <param name="dest"></param>
		/// <param name="peMode"></param>
		/// <param name="traverseParms"></param>
		private bool CheckCellBasedReachability(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParms)
		{
			IntVec3 foundCell = IntVec3.Invalid;
			mapping.map.floodFiller.FloodFill(start, (IntVec3 cell) => PassCheck(cell, mapping.map, traverseParms), delegate (IntVec3 cell)
			{
				VehiclePawn vehicle = traverseParms.pawn as VehiclePawn;
				if (VehicleReachabilityImmediate.CanReachImmediateVehicle(cell, dest, mapping.map, vehicle.VehicleDef, peMode))
				{
					foundCell = cell;
					return true;
				}
				return false;
			}, int.MaxValue, false, null);

			if (foundCell.IsValid)
			{
				if (CanUseCache(traverseParms.mode))
				{
					VehicleRegion validRegionAt = RegionGrid.GetValidRegionAt(foundCell);
					if (!(validRegionAt is null))
					{
						foreach (VehicleRegion startRegion in startingRegions)
						{
							cache.AddCachedResult(startRegion.Room, validRegionAt.Room, traverseParms, true);
						}
					}
				}
				return true;
			}
			if (CanUseCache(traverseParms.mode))
			{
				foreach (VehicleRegion startRegion in startingRegions)
				{
					foreach (VehicleRegion destRegion in destRegions)
					{
						cache.AddCachedResult(startRegion.Room, destRegion.Room, traverseParms, false);
					}
				}
			}
			return false;
		}

		private bool PassCheck(IntVec3 cell, Map map, TraverseParms traverseParms)
		{
			int num = map.cellIndices.CellToIndex(cell);
			if ((traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater || traverseParms.mode == TraverseMode.NoPassClosedDoorsOrWater) && cell.GetTerrain(map).IsWater)
			{
				return false;
			}
			if (traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater)
			{
				if (!PathGrid.WalkableFast(num))
				{
					Building edifice = cell.GetEdifice(map);
					if (edifice is null || !VehiclePathFinder.IsDestroyable(edifice))
					{
						return false;
					}
				}
			}
			else if (traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater)
			{
				Log.ErrorOnce("Do not use this method for non-cell based modes!", 938476762);
				if (!PathGrid.WalkableFast(num))
				{
					return false;
				}
			}
			VehicleRegion region = RegionGrid.DirectGrid[num];
			return region is null || region.Allows(traverseParms, false);
		}

		/// <summary>
		/// Can reach colony at cell <paramref name="c"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="vehicleDef"></param>
		public bool CanReachBase(IntVec3 cell, VehicleDef vehicleDef)
		{
			if (Current.ProgramState != ProgramState.Playing)
			{
				return CanReachVehicle(cell, MapGenerator.PlayerStartSpot, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors,
					Danger.Deadly, false));
			}
			if (!GenGridVehicles.Walkable(cell, vehicleDef, mapping.map))
			{
				return false;
			}
			Faction faction = mapping.map.ParentFaction ?? Faction.OfPlayer;
			List<Pawn> list = mapping.map.mapPawns.SpawnedPawnsInFaction(faction);
			foreach (Pawn p in list)
			{
				if (p.CanReach(cell, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
				{
					return true;
				}
			}
			TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false);
			if (faction == Faction.OfPlayer)
			{
				List<Building> allBuildingsColonist = mapping.map.listerBuildings.allBuildingsColonist;
				foreach (Building b in allBuildingsColonist)
				{
					if (CanReachVehicle(cell, b, PathEndMode.Touch, traverseParms))
					{
						return true;
					}
				}
			}
			else
			{
				List<Thing> list2 = mapping.map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
				foreach (Thing thing in list2)
				{
					if (thing.Faction == faction && CanReachVehicle(cell, thing, PathEndMode.Touch, traverseParms))
					{
						return true;
					}
				}
			}
			return CanReachBiggestMapEdgeRoom(cell);
		}

		/// <summary>
		/// Reachability to largest <see cref="VehicleRoom"/> touching map edge
		/// </summary>
		/// <param name="c"></param>
		public bool CanReachBiggestMapEdgeRoom(IntVec3 c)
		{
			VehicleRoom usableRoom = null;
			foreach (VehicleRoom room in RegionGrid.allRooms)
			{
				if (room.TouchesMapEdge)
				{
					if (usableRoom is null || room.RegionCount > usableRoom.RegionCount)
					{
						usableRoom = room;
					}
				}
			}
			return usableRoom != null && CanReachVehicle(c, usableRoom.Regions[0].AnyCell, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors,
				Danger.Deadly, false));
		}

		/// <summary>
		/// Can reach map edge from <paramref name="cell"/>
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="traverseParms"></param>
		public bool CanReachMapEdge(IntVec3 cell, TraverseParms traverseParms)
		{
			if (traverseParms.pawn is VehiclePawn vehicle)
			{
				if (!vehicle.Spawned)
				{
					return false;
				}
				if (vehicle.Map != mapping.map)
				{
					Log.Error($"Called CanReachMapEdge with vehicle not spawned on this map. Pawn's current map should have been used instead of this one. vehicle={vehicle} vehicle.Map={vehicle.Map} map={mapping.map}");
					return false;
				}
			}
			VehicleRegion region = VehicleGridsUtility.GetRegion(cell, mapping.map, createdFor, RegionType.Set_Passable);
			if (region is null)
			{
				return false;
			}
			if (region.Room.TouchesMapEdge)
			{
				return true;
			}
			bool entryCondition(VehicleRegion from, VehicleRegion r) => r.Allows(traverseParms, false);
			bool foundReg = false;
			bool regionProcessor(VehicleRegion r)
			{
				if (r.Room.TouchesMapEdge)
				{
					foundReg = true;
					return true;
				}
				return false;
			}
			VehicleRegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, 9999, RegionType.Set_Passable);
			return foundReg;
		}

		/// <summary>
		/// Can reach <paramref name="cell"/> with Unfogged constraint
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="traverseParms"></param>
		public bool CanReachUnfogged(IntVec3 cell, TraverseParms traverseParms)
		{
			if (traverseParms.pawn != null)
			{
				if (!traverseParms.pawn.Spawned)
				{
					return false;
				}
				if (traverseParms.pawn.Map != mapping.map)
				{
					Log.Error(string.Concat(new object[]
					{
						"Called CanReachUnfogged() with a pawn spawned not on this map. This means that we can't check his reachability here. Pawn's current map should have been used instead of this one. pawn=",
						traverseParms.pawn,
						" pawn.Map=",
						traverseParms.pawn.Map,
						" map=",
						mapping.map
					}));
					return false;
				}
			}
			if (!cell.InBounds(mapping.map))
			{
				return false;
			}
			if (!cell.Fogged(mapping.map))
			{
				return true;
			}
			VehicleRegion region = VehicleGridsUtility.GetRegion(cell, mapping.map, createdFor, RegionType.Set_Passable);
			if (region == null)
			{
				return false;
			}
			bool entryCondition(VehicleRegion from, VehicleRegion r) => r.Allows(traverseParms, false);
			bool foundReg = false;
			bool regionProcessor(VehicleRegion r)
			{
				if (!r.AnyCell.Fogged(mapping.map))
				{
					foundReg = true;
					return true;
				}
				return false;
			}
			VehicleRegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, 9999, RegionType.Set_Passable);
			return foundReg;
		}

		/// <summary>
		/// Can retrieve cached value for reachability
		/// </summary>
		/// <param name="mode"></param>
		private bool CanUseCache(TraverseMode mode)
		{
			return mode != TraverseMode.PassAllDestroyableThingsNotWater && mode != TraverseMode.NoPassClosedDoorsOrWater;
		}

		/// <summary>
		/// Not using generic pathfinding classes because traversal starts / ends from regions but traverses links with weights
		/// </summary>
		/// <returns>ChunkSet containing regions in order towards destination</returns>
		private class AStar
		{
			private const int Status_Invalid = -1;
			private const int Status_Open = 0;
			private const int Status_Closed = 1;

			private static HashSet<IntVec3> drawnCells = new HashSet<IntVec3>();

			private readonly PriorityQueue<Node, int> openQueue = new PriorityQueue<Node, int>();
			private readonly Dictionary<VehicleRegionLink, Node> nodes = new Dictionary<VehicleRegionLink, Node>();

			private readonly VehicleRegionCostCalculatorWrapper calculator;

			private readonly VehicleMapping mapping;
			private readonly VehicleDef vehicleDef;

			public AStar(VehicleMapping mapping, VehicleDef vehicleDef)
			{
				this.mapping = mapping;
				this.vehicleDef = vehicleDef;
				calculator = new VehicleRegionCostCalculatorWrapper(mapping, vehicleDef);
			}

			public bool LogRetraceAttempts { get; set; } = true;

			public Map Map => mapping.map;

			public ChunkList Run(VehicleRegion start, VehicleRegion destination, TraverseParms traverseParms, bool debugDrawSearch = false)
			{
				if (debugDrawSearch)
				{
					MarkRegionForDrawing(start, Map);
					TaskManager.SleepTillNextTick();
				}

				foreach (VehicleRegionLink regionLink in start.links)
				{
					VehicleRegion to = regionLink.GetOtherRegion(start);
					openQueue.Enqueue(CreateNodeFor(regionLink, start, to, 0), 0);
				}

				try
				{
					while (openQueue.Count > 0)
					{
						if (!openQueue.TryDequeue(out Node current, out int priority))
						{
							goto PathNotFound;
						}
						foreach (VehicleRegionLink neighbor in Neighbors(current))
						{
							if (nodes.TryGetValue(neighbor, out Node nodeCache) && !nodeCache.IsOpen)
							{
								if (LogRetraceAttempts) SmashLog.Error($"Attempting to queue node that is not open for {neighbor}.\nSkipping to avoid infinite loop.");
								continue;
							}
							Node node = CreateNode(current, neighbor);
							if (node.Passable && node.Allows(traverseParms))
							{
								//Cache node in back-traversal dict
								nodes[neighbor] = node;
								NodeRegions(current, neighbor, out VehicleRegion _, out VehicleRegion otherRegion);

								//Check if destination reached
								if (otherRegion == destination)
								{
									Log.Message("SOLVING");
									return SolvePath(start, destination, current);
								}
								//Queue for traversal of neighbors
								openQueue.Enqueue(node, node.cost + node.heuristicCost);

								if (debugDrawSearch)
								{
									//MarkRegionForDrawing(otherRegion, Map);
								}
							}
							else
							{
								node.status = Status_Invalid;
								nodes[neighbor] = node;
							}
						}
						current.status = Status_Closed;
						nodes[current.regionLink] = current;
					}
					PathNotFound:;
					Log.Error($"Unable to find path from {start} to {destination}.");
					return null;
				}
				finally
				{
					CleanUp();
				}
			}

			private Node CreateNode(Node current, VehicleRegionLink neighbor)
			{
				Map.debugDrawer.FlashCell(current.regionLink.anchor, colorPct: 0.25f, text: current.regionLink.anchor.ToString(), duration: 180);
				TaskManager.SleepTillNextTick();
				Map.debugDrawer.FlashCell(neighbor.anchor, colorPct: 1, text: neighbor.anchor.ToString(), duration: 180);
				if (!drawnCells.Add(neighbor.anchor))
				{
					Log.Error($"Revisited Node! {neighbor.anchor}");
				}
				TaskManager.SleepTillNextTick();

				NodeRegions(current, neighbor, out VehicleRegion inFacingRegion, out VehicleRegion otherRegion);

				MarkRegionForDrawing(inFacingRegion, Map, drawLinks: false);
				TaskManager.SleepTillNextTick();
				MarkRegionForDrawing(otherRegion, Map, drawLinks: false);
				TaskManager.SleepTillNextTick();

				int cost = inFacingRegion.WeightBetween(current.regionLink, neighbor).cost;
				return CreateNodeFor(neighbor, inFacingRegion, otherRegion, cost);
			}

			private Node CreateNodeFor(VehicleRegionLink regionLink, VehicleRegion from, VehicleRegion to, int cost)
			{
				int nodeIndex = Map.cellIndices.CellToIndex(regionLink.anchor);
				Node node = new Node(regionLink, from, to);
				node.cost = cost;
				node.heuristicCost = 0;// calculator.GetPathCostFromDestToRegion(nodeIndex);
				return node;
			}

			private IEnumerable<VehicleRegionLink> Neighbors(Node node)
			{
				foreach (VehicleRegionLink regionLink in node.regionA.links)
				{
					if (regionLink != node.regionLink && (!nodes.TryGetValue(regionLink, out Node neighborNode) || neighborNode.IsOpen))
					{
						yield return regionLink;
					}
				}
				foreach (VehicleRegionLink regionLink in node.regionB.links)
				{
					if (regionLink != node.regionLink && (!nodes.TryGetValue(regionLink, out Node neighborNode) || neighborNode.IsOpen))
					{
						yield return regionLink;
					}
				}
			}

			private void NodeRegions(Node current, VehicleRegionLink neighbor, out VehicleRegion inFacingRegion, out VehicleRegion otherRegion)
			{
				inFacingRegion = current.regionLink.GetInFacingRegion(neighbor);
				otherRegion = neighbor.GetOtherRegion(inFacingRegion);
			}

			private ChunkList SolvePath(VehicleRegion start, VehicleRegion destination, Node finalNode)
			{
				List<VehicleRegion> result = new List<VehicleRegion>();

				Node node = finalNode;
				VehicleRegion traversing = destination;
				result.Add(destination);
				while (traversing != start)
				{
					traversing = node.regionLink.GetOtherRegion(traversing);
					result.Add(traversing);
					node = nodes[node.regionLink];
				}
				result.Add(start);
				result.Reverse();

				if (!result[0].Equals(start))
				{
					SmashLog.Error($"BFS was unable to solve path from {start} to {destination}.");
				}
				return new ChunkList(result);
			}

			private void CleanUp()
			{
				openQueue.Clear();
				nodes.Clear();
			}

			protected struct Node
			{
				public VehicleRegionLink regionLink;

				public VehicleRegion regionA;
				public VehicleRegion regionB;
				
				public int cost;
				public int heuristicCost;
				public int status;

				//Initializes as newly opened node
				public Node(VehicleRegionLink regionLink, VehicleRegion regionA, VehicleRegion regionB)
				{
					this.regionLink = regionLink;

					this.regionA = regionA;
					this.regionB = regionB;
					
					cost = 0;
					heuristicCost = 0;
					status = Status_Open;
				}

				public bool IsOpen => status == Status_Open;

				public bool Passable => regionA != null && regionA.type.Passable() && regionB != null && regionB.type.Passable();

				public bool Allows(TraverseParms traverseParms) => regionA.Allows(traverseParms, false) && regionB.Allows(traverseParms, false);

				public override int GetHashCode()
				{
					return regionLink.anchor.GetHashCode();
				}
			}
		}
	}
}
