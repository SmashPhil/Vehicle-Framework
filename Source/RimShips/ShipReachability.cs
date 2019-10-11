using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.AI
{
    public class ShipReachability
    {
        public ShipReachability(Map map, MapExtension mapE)
        {
            this.map = map;
            this.mapExt = mapE;
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

        private void QueueNewOpenRegion(Region region)
        {
            if(region is null)
            {
                Log.ErrorOnce("Tried to queue null region (RimShips).", 881121, false);
                return;
            }
            if(region.reachedIndex == this.reachedIndex)
            {
                Log.ErrorOnce("Region is already reached; you can't open it. Region: " + region.ToString(), 719991, false);
                return;
            }
            this.openQueue.Enqueue(region);
            region.reachedIndex = this.reachedIndex;
            this.numRegionsOpened++;
        }

        private uint NewReachedIndex()
        {
            return this.reachedIndex++;
        }

        private void FinalizeCheck()
        {
            Log.Message("Finalize Check called");
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
            Log.Message("At canreach");
            if(this.working)
            {
                Log.ErrorOnce("Called CanReach() while working for Ships. This should never happen. Suppressing further errors.", 7312233, false);
                return false;
            }
            if(!(traverseParms.pawn is null))
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
            if(!dest.IsValid)
            {
                return false;
            }
            if(dest.HasThing && dest.Thing.Map != this.map)
            {
                return false;
            }
            if(!start.InBounds(this.map) || !dest.Cell.InBounds(this.map))
            {
                return false;
            }
            //Room check?
            if(traverseParms.mode == TraverseMode.PassAllDestroyableThings)
            {
                TraverseParms traverseParms2 = traverseParms;
                traverseParms.mode = TraverseMode.PassDoors;
                if(this.CanReachShip(start, dest, peMode, traverseParms2))
                {
                    return true;
                }
            }
            Log.Message("Check before: " + (this.mapExt is null));
            dest = (LocalTargetInfo)GenPathShip.ResolvePathMode(traverseParms.pawn, dest.ToTargetInfo(this.map), ref peMode, this.mapExt);
            this.working = true;
            bool result;
            try
            {
                this.pathGrid = mapExt.getShipPathGrid;
                this.regionGrid = this.map.regionGrid;
                this.reachedIndex += 1u;
                this.destRegions.Clear();
                if(peMode == PathEndMode.OnCell)
                {
                    Region region = dest.Cell.GetRegion(this.map, RegionType.Set_All);
                    if(!(region is null) && region.Allows(traverseParms, true))
                    {
                        this.destRegions.Add(region);
                    }
                }
                else if(peMode == PathEndMode.Touch)
                {
                    TouchPathEndModeUtility.AddAllowedAdjacentRegions(dest, traverseParms, this.map, this.destRegions);
                }
                if(this.destRegions.Count == 0 && traverseParms.mode != TraverseMode.PassAllDestroyableThings && traverseParms.mode !=
                    TraverseMode.PassAllDestroyableThingsNotWater)
                {
                    Log.Message("Am I supposed to be here?");
                    this.FinalizeCheck();
                    result = false;
                }
                else
                {
                    this.destRegions.RemoveDuplicates<Region>();
                    this.openQueue.Clear();
                    this.numRegionsOpened = 0;
                    this.DetermineStartRegions(start);
                    if(this.openQueue.Count == 0 && traverseParms.mode != TraverseMode.PassAllDestroyableThings && traverseParms.mode !=
                        TraverseMode.PassAllDestroyableThingsNotWater)
                    {
                        Log.Message("Here?");
                        this.FinalizeCheck();
                        result = false;
                    }
                    else
                    {
                        Log.Message("HERE");
                        //Fix Later
                        /*if(this.startingRegions.Any<Region>() && this.destRegions.Any<Region>() && this.CanUseCache(traverseParms.mode))
                        {
                            BoolUnknown cachedResult = this.GetCachedResult(traverseParms);
                            if (cachedResult == BoolUnknown.True)
							{
                                Log.Message("1");
								this.FinalizeCheck();
								return true;
							}
							if (cachedResult == BoolUnknown.False)
							{
                                Log.Message("2");
								this.FinalizeCheck();
								return false;
							}
							if (cachedResult != BoolUnknown.Unknown)
							{
							}
                        }*/
                        if(traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater || 
                            traverseParms.mode == TraverseMode.NoPassClosedDoorsOrWater)
                        {
                            Log.Message("Check Final1");
                            bool flag = this.CheckCellBasedReachability(start, dest, peMode, traverseParms);
                            this.FinalizeCheck();
                            result = flag;
                        }
                        else
                        {
                            Log.Message("Check Final2");
                            bool flag2 = this.CheckRegionBasedReachability(traverseParms);
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
            Log.Message("Result: " + result);
            return result;
        }

        private void DetermineStartRegions(IntVec3 start)
        {
            this.startingRegions.Clear();
            if(this.pathGrid.WalkableFast(start))
            {
                Region validRegionAt = this.regionGrid.GetValidRegionAt(start);
                this.QueueNewOpenRegion(validRegionAt);
                this.startingRegions.Add(validRegionAt);
            }
            else
            {
                for(int i = 0; i < 8; i++)
                {
                    IntVec3 c = start + GenAdj.AdjacentCells[i];
                    if(c.InBounds(this.map))
                    {
                        if(this.pathGrid.WalkableFast(c))
                        {
                            Region validRegionAt2 = this.regionGrid.GetValidRegionAt(c);
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
                Region region = this.openQueue.Dequeue();
                foreach(RegionLink regionLink in region.links)
                {
                    for(int i = 0; i < 2; i++)
                    {
                        Region region2 = regionLink.regions[i];
                        if(!(region2 is null) && region2.reachedIndex != this.reachedIndex && region2.type.Passable())
                        {
                            if(region2.Allows(traverseParms, false))
                            {
                                if(this.destRegions.Contains(region2))
                                {
                                    foreach(Region startRegion in this.startingRegions)
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
            foreach(Region startRegion in this.startingRegions)
            {
                foreach(Region destRegion in this.destRegions)
                {
                    this.cache.AddCachedResult(startRegion.Room, destRegion.Room, traverseParms, false);
                }
            }
            return false;
        }

        private bool CheckCellBasedReachability(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParms)
        {
            IntVec3 foundCell = IntVec3.Invalid;
            Region[] directionRegionGrid = this.regionGrid.DirectGrid;
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
                    Log.ErrorOnce("Do not use this method for non-cell based modes!", 938476762, false);
                    if (!pathGrid.WalkableFast(num))
                    {
                        return false;
                    }
                }
                Region region = directionRegionGrid[num];
                return region is null || region.Allows(traverseParms, false);
            }, delegate (IntVec3 c)
            {
                if (ReachabilityImmediate.CanReachImmediate(c, dest, this.map, peMode, traverseParms.pawn))
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
                    Region validRegionAt = this.regionGrid.GetValidRegionAt(foundCell);
                    if( !(validRegionAt is null) )
                    {
                        foreach(Region startRegion in this.startingRegions)
                        {
                            this.cache.AddCachedResult(startRegion.Room, validRegionAt.Room, traverseParms, true);
                        }
                    }
                }
                return true;
            }
            if(this.CanUseCache(traverseParms.mode))
            {
                foreach(Region startRegion in this.startingRegions)
                {
                    foreach(Region destRegion in this.destRegions)
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
            Region region = c.GetRegion(this.map, RegionType.Set_Passable);
            if (region == null)
            {
                return false;
            }
            if (region.Room.TouchesMapEdge)
            {
                return true;
            }
            RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParms, false);
            bool foundReg = false;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                if (r.Room.TouchesMapEdge)
                {
                    foundReg = true;
                    return true;
                }
                return false;
            };
            RegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, 9999, RegionType.Set_Passable);
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
            if (!c.InBounds(this.map))
            {
                return false;
            }
            if (!c.Fogged(this.map))
            {
                return true;
            }
            Region region = c.GetRegion(this.map, RegionType.Set_Passable);
            if (region == null)
            {
                return false;
            }
            RegionEntryPredicate entryCondition = (Region from, Region r) => r.Allows(traverseParms, false);
            bool foundReg = false;
            RegionProcessor regionProcessor = delegate (Region r)
            {
                if (!r.AnyCell.Fogged(this.map))
                {
                    foundReg = true;
                    return true;
                }
                return false;
            };
            RegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, 9999, RegionType.Set_Passable);
            return foundReg;
        }

        private bool CanUseCache(TraverseMode mode)
        {
            return mode != TraverseMode.PassAllDestroyableThingsNotWater && mode != TraverseMode.NoPassClosedDoorsOrWater;
        }

        private Map map;

        private Queue<Region> openQueue = new Queue<Region>();

        private List<Region> startingRegions = new List<Region>();

        private List<Region> destRegions = new List<Region>();

        private uint reachedIndex = 1u;

        private int numRegionsOpened;

        private bool working;

        private ReachabilityCache cache;

        private ShipPathGrid pathGrid;

        private RegionGrid regionGrid;

        private MapExtension mapExt;
    }
}
