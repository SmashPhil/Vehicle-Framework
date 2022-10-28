using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	/// <summary>
	/// Reachability calculator for quick result path finding before running the algorithm
	/// </summary>
	public sealed class VehicleReachability
	{
		private readonly Map map;
		private readonly VehicleDef vehicleDef;

		private readonly Queue<VehicleRegion> openQueue = new Queue<VehicleRegion>();

		private readonly List<VehicleRegion> startingRegions = new List<VehicleRegion>();
		private readonly List<VehicleRegion> destRegions = new List<VehicleRegion>();

		private uint reachedIndex = 1;

		private VehicleReachabilityCache cache = new VehicleReachabilityCache();

		private VehiclePathGrid pathGrid;
		private VehicleRegionGrid regionGrid;

		public VehicleReachability(Map map, VehicleDef vehicleDef)
		{
			this.map = map;
			this.vehicleDef = vehicleDef;
		}

		public void FinalizeInit()
		{
			//TODO - cache pathGrid and regionGrid
		}

		/// <summary>
		/// Currently calculating reachability between regions
		/// </summary>
		private bool CalculatingReachability { get; set; }

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
			return (dest.Map is null || dest.Map == map) && CanReachVehicle(start, (LocalTargetInfo)dest, peMode, traverseMode, maxDanger);
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
			return (dest.Map is null || dest.Map == map) && CanReachVehicle(start, (LocalTargetInfo)dest, peMode, traverseParms);
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
			if (CalculatingReachability)
			{
				Log.ErrorOnce("Called CanReachVehicle() while working. This should never happen. Suppressing further errors.", "CanReachVehicleWorkingError".GetHashCode());
				return false;
			}
			VehiclePawn vehicle = traverseParms.pawn as VehiclePawn;
			if (vehicle != null)
			{
				if (!vehicle.Spawned)
				{
					return false;
				}
				if (vehicle.Map != map)
				{
					Log.Error($"Called CanReach with a vehicle not spawned on this map. This means that we can't check its reachability here. Vehicle's current map should have been used instead. vehicle={vehicle} vehicle.Map={vehicle.Map} map={map}");
					return false;
				}
			}
			if (!dest.IsValid)
			{
				return false;
			}
			if (dest.HasThing && dest.Thing.Map != map)
			{
				return false;
			}
			if (!start.InBounds(map) || !dest.Cell.InBounds(map)) 
			{
				return false;
			}

			pathGrid = map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehiclePathGrid;
			regionGrid = map.GetCachedMapComponent<VehicleMapping>()[vehicleDef].VehicleRegionGrid;

			if (!pathGrid.WalkableFast(start))
			{
				return false;
			}
			bool freeTraversal = traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater && traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater;
			if ((peMode == PathEndMode.OnCell || peMode == PathEndMode.Touch || peMode == PathEndMode.ClosestTouch) && freeTraversal)
			{
				VehicleRoom room = VehicleRegionAndRoomQuery.RoomAtFast(start, map, vehicleDef, RegionType.Set_Passable);
				if (room != null && room == VehicleRegionAndRoomQuery.RoomAtFast(dest.Cell, map, vehicleDef, RegionType.Set_Passable))
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
			dest = (LocalTargetInfo)GenPathVehicles.ResolvePathMode(vehicle.VehicleDef, vehicle.Map, dest.ToTargetInfo(map), ref peMode);
			CalculatingReachability = true;
			bool result;
			try
			{
				reachedIndex += 1;
				destRegions.Clear();
				if (peMode == PathEndMode.OnCell)
				{
					VehicleRegion region = VehicleGridsUtility.GetRegion(dest.Cell, map, vehicleDef, RegionType.Set_Passable);
					if(region != null && region.Allows(traverseParms, true))
					{
						destRegions.Add(region);
					}
				}
				else if (peMode == PathEndMode.Touch)
				{
					TouchPathEndModeUtilityVehicles.AddAllowedAdjacentRegions(dest, traverseParms, map, vehicleDef, destRegions);
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
			if (pathGrid.WalkableFast(start))
			{
				VehicleRegion validRegionAt = regionGrid.GetValidRegionAt(start);
				QueueNewOpenRegion(validRegionAt);
				startingRegions.Add(validRegionAt);
			}
			else
			{
				for (int i = 0; i < 8; i++)
				{
					IntVec3 c = start + GenAdj.AdjacentCells[i];
					if (c.InBounds(map))
					{
						if (pathGrid.WalkableFast(c))
						{
							VehicleRegion validRegionAt = regionGrid.GetValidRegionAt(c);
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
					for(int i = 0; i < 2; i++)
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
			map.floodFiller.FloodFill(start, (IntVec3 cell) => PassCheck(cell, map, traverseParms), delegate (IntVec3 cell)
			{
				VehiclePawn vehicle = traverseParms.pawn as VehiclePawn;
				if (VehicleReachabilityImmediate.CanReachImmediateVehicle(cell, dest, map, vehicle.VehicleDef, peMode))
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
					VehicleRegion validRegionAt = regionGrid.GetValidRegionAt(foundCell);
					if( !(validRegionAt is null) )
					{
						foreach(VehicleRegion startRegion in startingRegions)
						{
							cache.AddCachedResult(startRegion.Room, validRegionAt.Room, traverseParms, true);
						}
					}
				}
				return true;
			}
			if (CanUseCache(traverseParms.mode))
			{
				foreach(VehicleRegion startRegion in startingRegions)
				{
					foreach(VehicleRegion destRegion in destRegions)
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
				if (!pathGrid.WalkableFast(num))
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
				if (!pathGrid.WalkableFast(num))
				{
					return false;
				}
			}
			VehicleRegion region = regionGrid.DirectGrid[num];
			return region is null || region.Allows(traverseParms, false);
		}

		/// <summary>
		/// Can reach colony at cell <paramref name="c"/>
		/// </summary>
		/// <param name="c"></param>
		public bool CanReachBase(IntVec3 c)
		{
			if (Current.ProgramState != ProgramState.Playing)
			{
				return CanReachVehicle(c, MapGenerator.PlayerStartSpot, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors,
					Danger.Deadly, false));
			}
			if (!GenGridVehicles.Walkable(c, vehicleDef, map))
			{
				return false;
			}
			Faction faction = map.ParentFaction ?? Faction.OfPlayer;
			List<Pawn> list = map.mapPawns.SpawnedPawnsInFaction(faction);
			foreach (Pawn p in list)
			{
				if (p.CanReach(c, PathEndMode.OnCell, Danger.Deadly, false, false, TraverseMode.ByPawn))
				{
					return true;
				}
			}
			TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false);
			if(faction == Faction.OfPlayer)
			{
				List<Building> allBuildingsColonist = map.listerBuildings.allBuildingsColonist;
				foreach (Building b in allBuildingsColonist)
				{
					if (CanReachVehicle(c, b, PathEndMode.Touch, traverseParms))
					{
						return true;
					}
				}
			}
			else
			{
				List<Thing> list2 = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
				foreach(Thing t in list2)
				{
					if(t.Faction == faction && CanReachVehicle(c, t, PathEndMode.Touch, traverseParms))
					{
						return true;
					}
				}
			}
			return CanReachBiggestMapEdgeRoom(c);
		}

		/// <summary>
		/// Reachability to largest <see cref="VehicleRoom"/> touching map edge
		/// </summary>
		/// <param name="c"></param>
		public bool CanReachBiggestMapEdgeRoom(IntVec3 c)
		{
			VehicleRoom usableRoom = null;
			foreach(VehicleRoom room in regionGrid.allRooms)
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
				if (vehicle.Map != map)
				{
					Log.Error($"Called CanReachMapEdge with vehicle not spawned on this map. Pawn's current map should have been used instead of this one. vehicle={vehicle} vehicle.Map={vehicle.Map} map={map}");
					return false;
				}
			}
			VehicleRegion region = VehicleGridsUtility.GetRegion(cell, map, vehicleDef, RegionType.Set_Passable);
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
				if (traverseParms.pawn.Map != map)
				{
					Log.Error(string.Concat(new object[]
					{
						"Called CanReachUnfogged() with a pawn spawned not on this map. This means that we can't check his reachability here. Pawn's current map should have been used instead of this one. pawn=",
						traverseParms.pawn,
						" pawn.Map=",
						traverseParms.pawn.Map,
						" map=",
						map
					}));
					return false;
				}
			}
			if (!cell.InBounds(map))
			{
				return false;
			}
			if (!cell.Fogged(map))
			{
				return true;
			}
			VehicleRegion region = VehicleGridsUtility.GetRegion(cell, map, vehicleDef, RegionType.Set_Passable);
			if (region == null)
			{
				return false;
			}
			bool entryCondition(VehicleRegion from, VehicleRegion r) => r.Allows(traverseParms, false);
			bool foundReg = false;
			bool regionProcessor(VehicleRegion r)
			{
				if (!r.AnyCell.Fogged(map))
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
	}
}
