using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace RimShips.AI
{
    public class ShipReachability
    {
        public ShipReachability(Map map)
        {
            this.map = map;
            this.mapExt = MapExtensionUtility.GetExtensionToMap(map);
        }
        
        public void ClearCache()
        {
            if(this.cache.Count > 0) { this.cache.Clear(); }
        }

        public void ClearCacheFor(Pawn pawn)
        {
            this.cache.ClearFor(pawn);
        }

        public void ClearCacheForHostile(Thing hostileTo)
        {
            this.cache.ClearForHostile(hostileTo);
        }

        private void QueueNewOpenRegion(WaterRegion region)
        {
            if(region is null)
            {
                Log.ErrorOnce("Tried to queue null region (RimShips).", 881121, false);
                return;
            }
            if(region.reachedIndex == this.reachedIndex)
            {
                Log.ErrorOnce("WaterRegion is already reached; you can't open it. WaterRegion: " + region.ToString(), 719991, false);
                return;
            }
            this.openQueue.Enqueue(region);
            region.reachedIndex = this.reachedIndex;
            this.numRegionsOpened++;
        }

        private void FinalizeCheck()
        {
            this.working = false;
        }

        public bool CanReachShipNonLocal(IntVec3 start, TargetInfo dest, PathEndMode peMode, TraverseMode traverseMode, Danger maxDanger)
        {
            return (dest.Map is null || dest.Map == this.map) && this.CanReachShip(start, (LocalTargetInfo)dest, peMode, traverseMode, maxDanger);
        }

        public bool CanReachShipNonLocal(IntVec3 start, TargetInfo dest, PathEndMode peMode, TraverseParms traverseParms)
        {
            return (dest.Map is null || dest.Map == this.map) && this.CanReachShip(start, (LocalTargetInfo)dest, peMode, traverseParms);
        }

        public bool CanReachShip(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseMode traverseMode, Danger maxDanger)
        {
            return this.CanReachShip(start, dest, peMode, TraverseParms.For(traverseMode, maxDanger, false));
        }

        public bool CanReachShip(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParms)
        {
            if(this.working)
            {
                Log.ErrorOnce("Called CanReach() while working for Ships. This should never happen. Suppressing further errors.", 7312233, false);
                return false;
            }
            if(!this.map.terrainGrid.TerrainAt(dest.Cell).IsWater)
            {
                return false;
            }
            if (!(traverseParms.pawn is null))
            {
                if(!traverseParms.pawn.Spawned)
                {
                    return false;
                }
                if(traverseParms.pawn.Map != this.map)
                {
                    Log.Error(string.Concat(new object[]
                    {
                        "Called CanReach() with a ship spawned not on this map. This means that we can't check its reachability here. Pawn's" +
                        "current map should have been used instead of this one. pawn=", traverseParms.pawn,
                        " pawn.Map=", traverseParms.pawn.Map,
                        " map=", this.map
                    }), false);
                    return false;
                }
            }
            if (!dest.IsValid)
            {
                return false;
            }
            if (dest.HasThing && dest.Thing.Map != this.map)
            {
                return false;
            }
            if (!start.InBoundsShip(this.map) || !dest.Cell.InBoundsShip(this.map)) 
            {
                return false;
            }
            if((peMode == PathEndMode.OnCell || peMode == PathEndMode.Touch || peMode == PathEndMode.ClosestTouch) && traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater &&
                traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater)
            {
                WaterRoom room = WaterRegionAndRoomQuery.RoomAtFast(start, this.map, RegionType.Set_Passable);
                if (!(room is null) && room == WaterRegionAndRoomQuery.RoomAtFast(dest.Cell, this.map, RegionType.Set_Passable))
                    return true;
            }
            if(traverseParms.mode == TraverseMode.PassAllDestroyableThings)
            {
                TraverseParms traverseParms2 = traverseParms;
                traverseParms.mode = TraverseMode.PassDoors;
                if(this.CanReachShip(start, dest, peMode, traverseParms2))
                {
                    return true;
                }
            }
            dest = (LocalTargetInfo)GenPathShip.ResolvePathMode(traverseParms.pawn, dest.ToTargetInfo(this.map), ref peMode, this.mapExt);
            this.working = true;
            bool result;
            try
            {
                this.pathGrid = mapExt.getShipPathGrid;
                this.regionGrid = this.mapExt.getWaterRegionGrid;
                this.reachedIndex += 1u;
                this.destRegions.Clear();
                if(peMode == PathEndMode.OnCell)
                {
                    WaterRegion region = WaterGridsUtility.GetRegion(dest.Cell, this.map, RegionType.Set_Passable);
                    if(!(region is null) && region.Allows(traverseParms, true))
                    {
                        this.destRegions.Add(region);
                    }
                }
                else if(peMode == PathEndMode.Touch)
                {
                    TouchPathEndModeUtilityShips.AddAllowedAdjacentRegions(dest, traverseParms, this.map, this.destRegions);
                }
                if(this.destRegions.Count == 0 && traverseParms.mode != TraverseMode.PassAllDestroyableThings && traverseParms.mode !=
                    TraverseMode.PassAllDestroyableThingsNotWater)
                {
                    this.FinalizeCheck();
                    result = false;
                }
                else
                {
                    this.destRegions.RemoveDuplicates<WaterRegion>();
                    this.openQueue.Clear();
                    this.numRegionsOpened = 0;
                    this.DetermineStartRegions(start);
                    if(this.openQueue.Count == 0 && traverseParms.mode != TraverseMode.PassAllDestroyableThings && traverseParms.mode !=
                        TraverseMode.PassAllDestroyableThingsNotWater)
                    {
                        this.FinalizeCheck();
                        result = false;
                    }
                    else
                    {
                        if (this.startingRegions.Any<WaterRegion>() && this.destRegions.Any<WaterRegion>() && this.CanUseCache(traverseParms.mode))
                        {
                            BoolUnknown cachedResult = this.GetCachedResult(traverseParms);
                            if (cachedResult == BoolUnknown.True)
                            {
                                this.FinalizeCheck();
                                return true;
                            }
                            if (cachedResult == BoolUnknown.False)
                            {
                                this.FinalizeCheck();
                                return false;
                            }
                        }
                        if (traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater || 
                            traverseParms.mode == TraverseMode.NoPassClosedDoorsOrWater)
                        {
                            bool flag = this.CheckCellBasedReachability(start, dest, peMode, traverseParms);
                            this.FinalizeCheck();
                            result = flag;
                        }
                        else
                        {
                            //bool flag2 = this.CheckRegionBasedReachability(traverseParms);
                            bool flag2 = this.CheckCellBasedReachability(start, dest, peMode, traverseParms); //Change back to region based later
                            this.FinalizeCheck();
                            result = flag2;
                        }
                    }
                }
            }
            finally
            {
                this.working = false;
            }
            return result;
        }

        private void DetermineStartRegions(IntVec3 start)
        {
            this.startingRegions.Clear();
            if(this.pathGrid.WalkableFast(start))
            {
                WaterRegion validRegionAt = this.regionGrid.GetValidRegionAt(start);
                this.QueueNewOpenRegion(validRegionAt);
                this.startingRegions.Add(validRegionAt);
            }
            else
            {
                for(int i = 0; i < 8; i++)
                {
                    IntVec3 c = start + GenAdj.AdjacentCells[i];
                    if(c.InBoundsShip(this.map))
                    {
                        if(this.pathGrid.WalkableFast(c))
                        {
                            WaterRegion validRegionAt2 = this.regionGrid.GetValidRegionAt(c);
                            if(!(validRegionAt2 is null) && validRegionAt2.reachedIndex != this.reachedIndex)
                            {
                                this.QueueNewOpenRegion(validRegionAt2);
                                this.startingRegions.Add(validRegionAt2);
                            }
                        }
                    }
                }
            }
        }

        private BoolUnknown GetCachedResult(TraverseParms traverseParms)
        {
            bool flag = false;
            for (int i = 0; i < this.startingRegions.Count; i++)
            {
                for (int j = 0; j < this.destRegions.Count; j++)
                {
                    if (this.destRegions[j] == this.startingRegions[i])
                    {
                        return BoolUnknown.True;
                    }
                    BoolUnknown boolUnknown = this.cache.CachedResultFor(this.startingRegions[i].Room, this.destRegions[j].Room, traverseParms);
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

        private bool CheckRegionBasedReachability(TraverseParms traverseParms)
        {
            while(this.openQueue.Count > 0)
            {
                WaterRegion region = this.openQueue.Dequeue();
                foreach(WaterRegionLink regionLink in region.links)
                {
                    for(int i = 0; i < 2; i++)
                    {
                        WaterRegion region2 = regionLink.regions[i];
                        if(!(region2 is null) && region2.reachedIndex != this.reachedIndex && region2.type.Passable())
                        {
                            if(region2.Allows(traverseParms, false))
                            {
                                if(this.destRegions.Contains(region2))
                                {
                                    foreach(WaterRegion startRegion in this.startingRegions)
                                    {
                                        this.cache.AddCachedResult(startRegion.Room, region2.Room, traverseParms, true);
                                    }
                                    return true;
                                }
                                this.QueueNewOpenRegion(region2);
                            }
                        }
                    }
                }
            }
            foreach(WaterRegion startRegion in this.startingRegions)
            {
                foreach(WaterRegion destRegion in this.destRegions)
                {
                    this.cache.AddCachedResult(startRegion.Room, destRegion.Room, traverseParms, false);
                }
            }
            return false;
        }

        private bool CheckCellBasedReachability(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParms)
        {
            IntVec3 foundCell = IntVec3.Invalid;
            WaterRegion[] directionRegionGrid = this.regionGrid.DirectGrid;
            ShipPathGrid pathGrid = mapExt.getShipPathGrid;
            CellIndices cellIndices = this.map.cellIndices;
            this.map.floodFiller.FloodFill(start, delegate (IntVec3 c)
            {
                int num = cellIndices.CellToIndex(c);
                if ((traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater || traverseParms.mode == TraverseMode.NoPassClosedDoorsOrWater) &&
                c.GetTerrain(this.map).IsWater)
                {
                    return false;
                }
                if (traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater)
                {
                    if (!pathGrid.WalkableFast(num))
                    {
                        Building edifice = c.GetEdifice(this.map);
                        if (edifice is null || !ShipPathFinder.IsDestroyable(edifice))
                        {
                            return false;
                        }
                    }
                }
                else if (traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater)
                {
                    //Log.ErrorOnce("Do not use this method for non-cell based modes!", 938476762, false);
                    if (!pathGrid.WalkableFast(num))
                    {
                        return false;
                    }
                }
                WaterRegion region = directionRegionGrid[num];
                return region is null || region.Allows(traverseParms, false);
            }, delegate (IntVec3 c)
            {
                if (ShipReachabilityImmediate.CanReachImmediateShip(c, dest, this.map, peMode, traverseParms.pawn))
                {
                    foundCell = c;
                    return true;
                }
                return false;
            }, int.MaxValue, false, null);

            if(foundCell.IsValid)
            {
                if(this.CanUseCache(traverseParms.mode))
                {
                    WaterRegion validRegionAt = this.regionGrid.GetValidRegionAt(foundCell);
                    if( !(validRegionAt is null) )
                    {
                        foreach(WaterRegion startRegion in this.startingRegions)
                        {
                            this.cache.AddCachedResult(startRegion.Room, validRegionAt.Room, traverseParms, true);
                        }
                    }
                }
                return true;
            }
            if(this.CanUseCache(traverseParms.mode))
            {
                foreach(WaterRegion startRegion in this.startingRegions)
                {
                    foreach(WaterRegion destRegion in this.destRegions)
                    {
                        this.cache.AddCachedResult(startRegion.Room, destRegion.Room, traverseParms, false);
                    }
                }
            }
            return false;
        }

        public bool CanReachColony(IntVec3 c)
        {
            return this.CanReachFactionBase(c, Faction.OfPlayer);
        }

        public bool CanReachFactionBase(IntVec3 c, Faction factionBaseFaction)
        {
            if(Current.ProgramState != ProgramState.Playing)
            {
                return this.CanReachShip(c, MapGenerator.PlayerStartSpot, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors,
                    Danger.Deadly, false));
            }
            if(!c.Walkable(this.map))
            {
                return false;
            }
            Faction faction = this.map.ParentFaction ?? Faction.OfPlayer;
            List<Pawn> list = this.map.mapPawns.SpawnedPawnsInFaction(faction);
            foreach(Pawn p in list)
            {
                if(p.CanReach(c, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn))
                {
                    return true;
                }
            }
            TraverseParms traverseParms = TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false);
            if(faction == Faction.OfPlayer)
            {
                List<Building> allBuildingsColonist = this.map.listerBuildings.allBuildingsColonist;
                foreach(Building b in allBuildingsColonist)
                {
                    if(this.CanReachShip(c, b, PathEndMode.Touch, traverseParms))
                    {
                        return true;
                    }
                }
            }
            else
            {
                List<Thing> list2 = this.map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial);
                foreach(Thing t in list2)
                {
                    if(t.Faction == faction && this.CanReachShip(c, t, PathEndMode.Touch, traverseParms))
                    {
                        return true;
                    }
                }
            }
            return this.CanReachBiggestMapEdgeRoom(c);
        }

        public bool CanReachBiggestMapEdgeRoom(IntVec3 c)
        {
            Room room0 = null;
            foreach(Room room1 in this.map.regionGrid.allRooms)
            {
                Room room2 = room1;
                if(room2.TouchesMapEdge)
                {
                    if(room0 is null || room2.RegionCount > room0.RegionCount)
                    {
                        room0 = room2;
                    }
                }
            }
            return !(room0 is null) && this.CanReachShip(c, room0.Regions[0].AnyCell, PathEndMode.OnCell, TraverseParms.For(TraverseMode.PassDoors,
                Danger.Deadly, false));
        }

        public bool CanReachMapEdge(IntVec3 c, TraverseParms traverseParms)
        {
            if (!(traverseParms.pawn is null))
            {
                if (!traverseParms.pawn.Spawned)
                {
                    return false;
                }
                if (traverseParms.pawn.Map != this.map)
                {
                    Log.Error(string.Concat(new object[]
                    {
                        "Called CanReachMapEdge() with a pawn spawned not on this map. This means that we can't check his reachability here. Pawn's current map should have been used instead of this one. pawn=",
                        traverseParms.pawn,
                        " pawn.Map=",
                        traverseParms.pawn.Map,
                        " map=",
                        this.map
                    }), false);
                    return false;
                }
            }
            WaterRegion region = WaterGridsUtility.GetRegion(c, this.map, RegionType.Set_Passable);
            if (region is null)
                return false;
            if (region.Room.TouchesMapEdge)
                return true;
            WaterRegionEntryPredicate entryCondition = (WaterRegion from, WaterRegion r) => r.Allows(traverseParms, false);
            bool foundReg = false;
            WaterRegionProcessor regionProcessor = delegate (WaterRegion r)
            {
                if (r.Room.TouchesMapEdge)
                {
                    foundReg = true;
                    return true;
                }
                return false;
            };
            WaterRegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, 9999, RegionType.Set_Passable);
            return foundReg;
        }

        public bool CanReachUnfogged(IntVec3 c, TraverseParms traverseParms)
        {
            if (traverseParms.pawn != null)
            {
                if (!traverseParms.pawn.Spawned)
                {
                    return false;
                }
                if (traverseParms.pawn.Map != this.map)
                {
                    Log.Error(string.Concat(new object[]
                    {
                        "Called CanReachUnfogged() with a pawn spawned not on this map. This means that we can't check his reachability here. Pawn's current map should have been used instead of this one. pawn=",
                        traverseParms.pawn,
                        " pawn.Map=",
                        traverseParms.pawn.Map,
                        " map=",
                        this.map
                    }), false);
                    return false;
                }
            }
            if (!c.InBoundsShip(this.map))
            {
                return false;
            }
            if (!c.Fogged(this.map))
            {
                return true;
            }
            WaterRegion region = WaterGridsUtility.GetRegion(c, this.map, RegionType.Set_Passable);
            if (region == null)
            {
                return false;
            }
            WaterRegionEntryPredicate entryCondition = (WaterRegion from, WaterRegion r) => r.Allows(traverseParms, false);
            bool foundReg = false;
            WaterRegionProcessor regionProcessor = delegate (WaterRegion r)
            {
                if (!r.AnyCell.Fogged(this.map))
                {
                    foundReg = true;
                    return true;
                }
                return false;
            };
            WaterRegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, 9999, RegionType.Set_Passable);
            return foundReg;
        }

        private bool CanUseCache(TraverseMode mode)
        {
            return false;
            //return mode != TraverseMode.PassAllDestroyableThingsNotWater && mode != TraverseMode.NoPassClosedDoorsOrWater;
        }

        private Map map;

        private Queue<WaterRegion> openQueue = new Queue<WaterRegion>();

        private List<WaterRegion> startingRegions = new List<WaterRegion>();

        private List<WaterRegion> destRegions = new List<WaterRegion>();

        private uint reachedIndex = 1u;

        private int numRegionsOpened;

        private bool working;

        private ShipReachabilityCache cache = new ShipReachabilityCache();

        private ShipPathGrid pathGrid;

        private WaterRegionGrid regionGrid;

        private MapExtension mapExt;
    }
}
