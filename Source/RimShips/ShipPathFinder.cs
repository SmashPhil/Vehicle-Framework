using System;
using System.Collections.Generic;
using System.Diagnostics;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vehicles.AI
{
    public class ShipPathFinder
    {
        public ShipPathFinder(Map map)
        {
            this.map = map;
            this.mapE = MapExtensionUtility.GetExtensionToMap(map);
            this.mapSizeX = map.Size.x;
            this.mapSizeZ = map.Size.z;
            this.calcGrid = new ShipPathFinder.ShipPathFinderNodeFast[this.mapSizeX * this.mapSizeZ];
            this.openList = new FastPriorityQueue<ShipPathFinder.CostNode>(new ShipPathFinder.CostNodeComparer());
            this.regionCostCalculator = new RegionCostCalculatorWrapperShips(map);
        }

        public bool PawnIsShip(Pawn p)
        {
            return !(p.TryGetComp<CompVehicle>() is null) ? true : false;
        }

        public PawnPath FindShipPath(IntVec3 start, LocalTargetInfo dest, Pawn pawn, PathEndMode peMode = PathEndMode.OnCell)
        {
            /*bool flag = false;
            if(!(pawn is null) && PawnIsShip(pawn) && !(pawn.CurJob is null))
            {
                flag = true;
            }*/
            //can bash?
            Danger maxDanger = Danger.Deadly;
            return this.FindShipPath(start, dest, TraverseParms.For(pawn, maxDanger, TraverseMode.ByPawn, false), peMode);
        }

        public PawnPath FindShipPath(IntVec3 start, LocalTargetInfo dest, TraverseParms traverseParms, PathEndMode peMode = PathEndMode.OnCell)
        {
            if(DebugSettings.pathThroughWalls)
            {
                traverseParms.mode = TraverseMode.PassAllDestroyableThings;
            }
            Pawn pawn = traverseParms.pawn;
            if (!(pawn is null) && pawn.Map != this.map)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Tried to FindShipPath for pawn which is spawned in another map. his map ShipPathFinder should  have been used, not this one. " +
                    "pawn=", pawn,
                    " pawn.Map=", pawn.Map,
                    " map=", this.map
                }), false);
                return PawnPath.NotFound;
            }
            if(!start.IsValid)
            {
                Log.Error(string.Concat(new object[]
                {
                    "Tried to FindShipPath with invalid start ",
                    start,
                    ", pawn=", pawn
                }), false);
                return PawnPath.NotFound;
            }
            if (!dest.IsValid)
            {
                Log.Error(string.Concat(new object[]
                {
            "Tried to FindPath with invalid dest ",
            dest,
            ", pawn= ",
            pawn
                }), false);
                return PawnPath.NotFound;
            }
            if(traverseParms.mode == TraverseMode.ByPawn)
            {
                if(!ShipReachabilityUtility.CanReachShip(pawn, dest, peMode, Danger.Deadly, false, traverseParms.mode))
                {
                    return PawnPath.NotFound;
                }
            }
            else if(!mapE.getShipReachability.CanReachShip(start, dest, peMode, traverseParms))
            {
                return PawnPath.NotFound;
            }
            this.PfProfilerBeginSample(string.Concat(new object[]
            {
                "FindPath for ", pawn,
                " from ", start,
                " to ", dest,
                (!dest.HasThing) ? string.Empty : (" at " + dest.Cell)
            }));
            this.cellIndices = this.map.cellIndices;
            this.shipPathGrid = mapE.getShipPathGrid;
            this.edificeGrid = this.map.edificeGrid.InnerArray;
            this.blueprintGrid = this.map.blueprintGrid.InnerArray;
            int x = dest.Cell.x;
            int z = dest.Cell.z;
            int num = this.cellIndices.CellToIndex(start);
            int num2 = this.cellIndices.CellToIndex(dest.Cell);
            ByteGrid byteGrid = (pawn is null) ? null : pawn.GetAvoidGrid(true);
            bool flag = traverseParms.mode == TraverseMode.PassAllDestroyableThings || traverseParms.mode == TraverseMode.PassAllDestroyableThingsNotWater;
            bool flag2 = traverseParms.mode != TraverseMode.NoPassClosedDoorsOrWater && traverseParms.mode != TraverseMode.PassAllDestroyableThingsNotWater;
            bool flag3 = !flag;
            CellRect cellRect = this.CalculateDestinationRect(dest, peMode);
            bool flag4 = cellRect.Width == 1 && cellRect.Height == 1;
            int[] array = mapE.getShipPathGrid.pathGrid;
            TerrainDef[] topGrid = this.map.terrainGrid.topGrid;
            EdificeGrid edificeGrid = this.map.edificeGrid;
            int num3 = 0;
            int num4 = 0;
            Area allowedArea = this.GetAllowedArea(pawn);
            bool flag5 = !(pawn is null) && PawnUtility.ShouldCollideWithPawns(pawn);
            bool flag6 = true && DebugViewSettings.drawPaths;
            bool flag7 = !flag && !(WaterGridsUtility.GetRegion(start, this.map, RegionType.Set_Passable) is null) && flag2;
            bool flag8 = !flag || !flag3;
            bool flag9 = false;
            bool flag10 = !(pawn is null) && pawn.Drafted;
            bool flag11 = !(pawn is null) && !(pawn.GetComp<CompVehicle>() is null);

            int num5 = (!flag11) ? NodesToOpenBeforeRegionbasedPathing_NonShip : NodesToOpenBeforeRegionBasedPathing_Ship;
            int num6 = 0;
            int num7 = 0;
            float num8 = this.DetermineHeuristicStrength(pawn, start, dest);
            int num9 = !(pawn is null) ? pawn.TicksPerMoveCardinal : DefaultMoveTicksCardinal;
            int num10 = !(pawn is null) ? pawn.TicksPerMoveDiagonal : DefaultMoveTicksDiagonal;

            this.CalculateAndAddDisallowedCorners(traverseParms, peMode, cellRect);
            this.InitStatusesAndPushStartNode(ref num, start);
            for(;;)
            {
                this.PfProfilerBeginSample("Open cell");
                if(this.openList.Count <= 0)
                {
                    break;
                }
                num6 += this.openList.Count;
                num7++;
                ShipPathFinder.CostNode costNode = this.openList.Pop();
                num = costNode.index;
                if(costNode.cost != this.calcGrid[num].costNodeCost)
                {
                    this.PfProfilerEndSample();
                }
                else if(this.calcGrid[num].status == this.statusClosedValue)
                {
                    this.PfProfilerEndSample();
                }
                else
                {
                    IntVec3 c = this.cellIndices.IndexToCell(num);
                    int x2 = c.x;
                    int z2 = c.z;
                    if(flag6)
                    {
                        this.DebugFlash(c, (float)this.calcGrid[num].knownCost / 1500f, this.calcGrid[num].knownCost.ToString());
                    }
                    if(flag4)
                    {
                        if(num == num2)
                        {
                            goto Block_32;
                        }
                    }
                    else if(cellRect.Contains(c) && !this.disallowedCornerIndices.Contains(num))
                    {
                        goto Block_34;
                    }
                    if(num3 > SearchLimit)
                    {
                        goto Block_35;
                    }
                    this.PfProfilerEndSample();
                    this.PfProfilerBeginSample("Neighbor consideration");
                    for(int i = 0; i < 8; i++)
                    {
                        uint num11 = (uint)(x2 + ShipPathFinder.Directions[i]);
                        uint num12 = (uint)(z2 + ShipPathFinder.Directions[i+8]);
                        if((ulong)num11 < ((ulong)this.mapSizeX) && (ulong)num12 < (ulong)((long)this.mapSizeZ))
                        {
                            int num13 = (int)num11;
                            int num14 = (int)num12;
                            int num15 = this.cellIndices.CellToIndex(num13, num14);
                            if(this.calcGrid[num15].status != this.statusClosedValue || flag9)
                            {
                                int num16 = 0;
                                //bool flag12 = false; Extra cost for traversing water

                                if(flag2 || !new IntVec3(num13, 0 ,num14).GetTerrain(this.map).HasTag("Water"))
                                {
                                    if(!this.shipPathGrid.WalkableFast(num15))
                                    {
                                        if(!flag)
                                        {
                                            if(flag6)
                                            {
                                                this.DebugFlash(new IntVec3(num13, 0, num14), 0.22f, "walk");
                                            }
                                            goto IL_E3A;
                                        }
                                        //flag12 = true;
                                        num16 += 70;
                                        Building building = edificeGrid[num15];
                                        if (building is null)
                                        {
                                            goto IL_E3A;
                                        }
                                        if(!IsDestroyable(building))
                                        {
                                            goto IL_E3A;
                                        }
                                        num16 += (int)((float)building.HitPoints * 0.2f);
                                    }
                                    if(i > 3)
                                    {
                                        switch(i)
                                        {
                                            case 4:
                                                if(this.BlocksDiagonalMovement(num - this.mapSizeX))
                                                {
                                                    if(flag8)
                                                    {
                                                        if(flag6)
                                                        {
                                                            this.DebugFlash(new IntVec3(x2, 0, z2 - 1), 0.9f, "ships");
                                                        }
                                                        goto IL_E3A;
                                                    }
                                                    num16 += 70;
                                                }
                                                if(this.BlocksDiagonalMovement(num+1))
                                                {
                                                    if(flag8)
                                                    {
                                                        if(flag6)
                                                        {
                                                            this.DebugFlash(new IntVec3(x2 + 1, 0, z2), 0.9f, "ships");
                                                        }
                                                        goto IL_E3A;
                                                    }
                                                    num16 += 70;
                                                }
                                                break;
                                            case 5:
                                                if(this.BlocksDiagonalMovement(num + this.mapSizeX))
                                                {
                                                    if(flag8)
                                                    {
                                                        if(flag6)
                                                        {
                                                            this.DebugFlash(new IntVec3(x2, 0, z2 + 1), 0.9f, "ships");
                                                        }
                                                        goto IL_E3A;
                                                    }
                                                    num16 += 70;
                                                }
                                                if(this.BlocksDiagonalMovement(num+1))
                                                {
                                                    if(flag8)
                                                    {
                                                        if(flag6)
                                                        {
                                                            this.DebugFlash(new IntVec3(x2 + 1, 0, z2), 0.9f, "ships");
                                                        }
                                                        goto IL_E3A;
                                                    }
                                                    num16 += 70;
                                                }
                                                break;
                                            case 6:
                                                if(this.BlocksDiagonalMovement(num + this.mapSizeX))
                                                {
                                                    if(flag8)
                                                    {
                                                        if(flag6)
                                                        {
                                                            this.DebugFlash(new IntVec3(x2, 0, z2 + 1), 0.9f, "ships");
                                                        }
                                                        goto IL_E3A;
                                                    }
                                                    num16 += 70;
                                                }
                                                if(this.BlocksDiagonalMovement(num - 1))
                                                {
                                                    if(flag8)
                                                    {
                                                        if(flag6)
                                                        {
                                                            this.DebugFlash(new IntVec3(x2 - 1, 0, z2), 0.9f, "ships");
                                                        }
                                                        goto IL_E3A;
                                                    }
                                                    num16 += 70;
                                                }
                                                break;
                                            case 7:
                                                if(this.BlocksDiagonalMovement(num - this.mapSizeX))
                                                {
                                                    if(flag8)
                                                    {
                                                        if(flag6)
                                                        {
                                                            this.DebugFlash(new IntVec3(x2, 0, z2 - 1), 0.9f, "ships");
                                                        }
                                                        goto IL_E3A;
                                                    }
                                                    num16 += 70;
                                                }
                                                if(this.BlocksDiagonalMovement(num - 1))
                                                {
                                                    if(flag8)
                                                    {
                                                        if(flag6)
                                                        {
                                                            this.DebugFlash(new IntVec3(x2 - 1, 0, z2), 0.9f, "ships");
                                                        }
                                                        goto IL_E3A;
                                                    }
                                                    num16 += 70;
                                                }
                                                break;
                                        }
                                    }
                                    int num17 = (i <= 3) ? num9 : num10;
                                    num17 += num16;
                                    /*if(!flag12)
                                    {
                                        //Extra Costs for traversing water
                                        num17 += array[num15];
                                        num17 += flag10 ? topGrid[num15].extraDraftedPerceivedPathCost : topGrid[num15].extraNonDraftedPerceivedPathCost;
                                    }*/
                                    if (!(byteGrid is null))
                                    {
                                        num17 += (int)(byteGrid[num15] * 8);
                                    }
                                    //Allowed area cost?
                                    if(flag5 && PawnUtility.AnyPawnBlockingPathAt(new IntVec3(num13, 0, num14), pawn, false, false, true))
                                    {
                                        num17 += Cost_PawnCollision;
                                    }
                                    Building building2 = this.edificeGrid[num15];
                                    if(!(building2 is null))
                                    {
                                        //Building Costs Here
                                    }
                                    List<Blueprint> list = this.blueprintGrid[num15];
                                    if(!(list is null))
                                    {
                                        this.PfProfilerBeginSample("Blueprints");
                                        int num18 = 0;
                                        foreach(Blueprint bp in list)
                                        {
                                            num18 = Mathf.Max(num18, GetBlueprintCost(bp, pawn));
                                        }
                                        if(num18 == int.MaxValue)
                                        {
                                            this.PfProfilerEndSample();
                                            goto IL_E3A;
                                        }
                                        num17 += num18;
                                        this.PfProfilerEndSample();
                                    }
                                    int num19 = num17 + this.calcGrid[num].knownCost;
                                    ushort status = this.calcGrid[num15].status;
                                    if (!(pawn.GetComp<CompVehicle>() is null) && !this.map.terrainGrid.TerrainAt(num15).IsWater)
                                        num19 += 10000;
                                    if (status == this.statusClosedValue || status == this.statusOpenValue)
                                    {
                                        int num20 = 0;
                                        if(status == this.statusClosedValue)
                                        {
                                            num20 = num9;
                                        }
                                        if(this.calcGrid[num15].knownCost <= num19 + num20)
                                        {
                                            goto IL_E3A;
                                        }
                                    }
                                    if(flag9)
                                    {
                                        this.calcGrid[num15].heuristicCost = Mathf.RoundToInt((float)this.regionCostCalculator.GetPathCostFromDestToRegion(num15) *
                                            ShipPathFinder.RegionheuristicWeighByNodesOpened.Evaluate((float)num4));
                                        if(this.calcGrid[num15].heuristicCost < 0)
                                        {
                                            Log.ErrorOnce(string.Concat(new object[]
                                            {
                                                "Heuristic cost overflow for ship ", pawn.ToStringSafe<Pawn>(),
                                                " pathing from ", start,
                                                " to ", dest, "."
                                            }), pawn.GetHashCode() ^ 193840009, false);
                                            this.calcGrid[num15].heuristicCost = 0;
                                        }
                                    }
                                    else if(status != this.statusClosedValue && status != this.statusOpenValue)
                                    {
                                        int dx = Math.Abs(num13 - x);
                                        int dz = Math.Abs(num14 - z);
                                        int num21 = GenMath.OctileDistance(dx, dz, num9, num10);
                                        this.calcGrid[num15].heuristicCost = Mathf.RoundToInt((float)num21 * num8);
                                    }
                                    int num22 = num19 + this.calcGrid[num15].heuristicCost;
                                    if(num22 < 0)
                                    {
                                        Log.ErrorOnce(string.Concat(new object[]
                                        {
                                            "Node cost overflow for ship ", pawn.ToStringSafe<Pawn>(),
                                            " pathing from ", start,
                                            " to ", dest, "."
                                        }), pawn.GetHashCode() ^ 87865822, false);
                                        num22 = 0;
                                    }
                                    this.calcGrid[num15].parentIndex = num;
                                    this.calcGrid[num15].knownCost = num19;
                                    this.calcGrid[num15].status = this.statusOpenValue;
                                    this.calcGrid[num15].costNodeCost = num22;
                                    num4++;
                                    this.openList.Push(new ShipPathFinder.CostNode(num15, num22));
                                }
                            }
                        }
                        IL_E3A:;
                    }
                    this.PfProfilerEndSample();
                    num3++;
                    this.calcGrid[num].status = this.statusClosedValue;
                    if(num4 >= num5 && flag7 && !flag9)
                    {
                        flag9 = true;
                        this.regionCostCalculator.Init(cellRect, traverseParms, num9, num10, byteGrid, allowedArea, flag10,
                            this.disallowedCornerIndices);
                        this.InitStatusesAndPushStartNode(ref num, start);
                        num4 = 0;
                        num3 = 0;
                    }
                }
            }
            string text = ((pawn is null) || pawn.CurJob is null) ? "null" : pawn.CurJob.ToString();
            string text2 = ((pawn is null) || pawn.Faction is null) ? "null" : pawn.Faction.ToString();
            Log.Warning(string.Concat(new object[]
            {
                "ship pawn: ", pawn, " pathing from ", start,
                " to ", dest, " ran out of cells to process.\nJob:", text,
                "\nFaction: ", text2
            }), false);
            this.DebugDrawRichData();
            this.PfProfilerEndSample();
            return PawnPath.NotFound;
            Block_32:
            this.PfProfilerEndSample();
            PawnPath result = this.FinalizedPath(num, flag9);
            this.PfProfilerEndSample();
            return result;
            Block_34:
            this.PfProfilerEndSample();
            PawnPath result2 = this.FinalizedPath(num, flag9);
            this.PfProfilerEndSample();
            return result2;
            Block_35:
            Log.Warning(string.Concat(new object[]
            {
                "Ship ", pawn, " pathing from ", start,
                " to ", dest, " hit search limit of ", SearchLimit, " cells."
            }), false);
            this.DebugDrawRichData();
            this.PfProfilerEndSample();
            return PawnPath.NotFound;
        }

        public static int GetBuildingCost(Building b, TraverseParms traverseParms, Pawn pawn)
        {
            Building_Door building_Door = b as Building_Door;
            if (building_Door != null)
            {
                switch (traverseParms.mode)
                {
                    case TraverseMode.ByPawn:
                        if (!traverseParms.canBash && building_Door.IsForbiddenToPass(pawn))
                        {
                            if (DebugViewSettings.drawPaths)
                            {
                                ShipPathFinder.DebugFlash(b.Position, b.Map, 0.77f, "forbid");
                            }
                            return int.MaxValue;
                        }
                        if (building_Door.PawnCanOpen(pawn) && !building_Door.FreePassage)
                        {
                            return building_Door.TicksToOpenNow;
                        }
                        if (building_Door.CanPhysicallyPass(pawn))
                        {
                            return 0;
                        }
                        if (traverseParms.canBash)
                        {
                            return 300;
                        }
                        if (DebugViewSettings.drawPaths)
                        {
                            ShipPathFinder.DebugFlash(b.Position, b.Map, 0.34f, "cant pass");
                        }
                        return int.MaxValue;
                    case TraverseMode.PassDoors:
                        if (pawn != null && building_Door.PawnCanOpen(pawn) && !building_Door.IsForbiddenToPass(pawn) && !building_Door.FreePassage)
                        {
                            return building_Door.TicksToOpenNow;
                        }
                        if ((pawn != null && building_Door.CanPhysicallyPass(pawn)) || building_Door.FreePassage)
                        {
                            return 0;
                        }
                        return 150;
                    case TraverseMode.NoPassClosedDoors:
                    case TraverseMode.NoPassClosedDoorsOrWater:
                        if (building_Door.FreePassage)
                        {
                            return 0;
                        }
                        return int.MaxValue;
                    case TraverseMode.PassAllDestroyableThings:
                    case TraverseMode.PassAllDestroyableThingsNotWater:
                        if (pawn != null && building_Door.PawnCanOpen(pawn) && !building_Door.IsForbiddenToPass(pawn) && !building_Door.FreePassage)
                        {
                            return building_Door.TicksToOpenNow;
                        }
                        if ((pawn != null && building_Door.CanPhysicallyPass(pawn)) || building_Door.FreePassage)
                        {
                            return 0;
                        }
                        return 50 + (int)((float)building_Door.HitPoints * 0.2f);
                }
            }
            else if (pawn != null)
            {
                return (int)b.PathFindCostFor(pawn);
            }
            return 0;
        }

        public static int GetBlueprintCost(Blueprint b, Pawn pawn)
        {
            if(!(pawn is null))
            {
                return (int)b.PathFindCostFor(pawn);
            }
            return 0;
        }

        public static bool IsDestroyable(Thing t)
        {
            return t.def.useHitPoints && t.def.destroyable;
        }

        private bool BlocksDiagonalMovement(int x, int z)
        {
            return ShipPathFinder.BlocksDiagonalMovement(x, z, this.map, mapE);
        }

        private bool BlocksDiagonalMovement(int index)
        {
            return ShipPathFinder.BlocksDiagonalMovement(index, this.map, mapE);
        }

        public static bool BlocksDiagonalMovement(int x, int z, Map map, MapExtension mapE)
        {
            return ShipPathFinder.BlocksDiagonalMovement(map.cellIndices.CellToIndex(x, z), map, mapE);
        }

        public static bool BlocksDiagonalMovement(int index, Map map, MapExtension mapE)
        {
            return !mapE.getShipPathGrid.WalkableFast(index) || map.edificeGrid[index] is Building_Door;
        }

        private void DebugFlash(IntVec3 c, float colorPct, string str)
        {
            ShipPathFinder.DebugFlash(c, this.map, colorPct, str);
        }

        private static void DebugFlash(IntVec3 c, Map map, float colorPct, string str)
        {
            map.debugDrawer.FlashCell(c, colorPct, str, 50);
        }

        private PawnPath FinalizedPath(int finalIndex, bool usedRegionHeuristics)
        {
            PawnPath emptyPawnPath = this.map.pawnPathPool.GetEmptyPawnPath();
            int num = finalIndex;
            for(; ;)
            {
                ShipPathFinder.ShipPathFinderNodeFast shipPathFinderNodeFast = this.calcGrid[num];
                int parentIndex = shipPathFinderNodeFast.parentIndex;
                emptyPawnPath.AddNode(this.map.cellIndices.IndexToCell(num));
                if(num == parentIndex)
                {
                    break;
                }
                num = parentIndex;
            }
            emptyPawnPath.SetupFound((float)this.calcGrid[finalIndex].knownCost, usedRegionHeuristics);
            return emptyPawnPath;
        }

        private void InitStatusesAndPushStartNode(ref int curIndex, IntVec3 start)
        {
            this.statusOpenValue += 2;
            this.statusClosedValue += 2;
            if (this.statusClosedValue >= 65435)
            {
                this.ResetStatuses();
            }
            curIndex = this.cellIndices.CellToIndex(start);
            this.calcGrid[curIndex].knownCost = 0;
            this.calcGrid[curIndex].heuristicCost = 0;
            this.calcGrid[curIndex].costNodeCost = 0;
            this.calcGrid[curIndex].parentIndex = curIndex;
            this.calcGrid[curIndex].status = this.statusOpenValue;
            this.openList.Clear();
            this.openList.Push(new ShipPathFinder.CostNode(curIndex, 0));
        }

        private void ResetStatuses()
        {
            int num = this.calcGrid.Length;
            for(int i = 0; i < num; i++)
            {
                this.calcGrid[i].status = 0;
            }
            this.statusOpenValue = 1;
            this.statusClosedValue = 2;
        }

        [Conditional("PFPROFILE_SHIPS")]
        private void PfProfilerBeginSample(string s)
        {
        }
        [Conditional("PFPROFILE_SHIPS")]
        private void PfProfilerEndSample()
        {
        }
        private void DebugDrawRichData()
        {
            if(DebugViewSettings.drawPaths)
            {
                while(this.openList.Count > 0)
                {
                    int index = this.openList.Pop().index;
                    IntVec3 c = new IntVec3(index % this.mapSizeX, 0, index / this.mapSizeX);
                    this.map.debugDrawer.FlashCell(c, 0f, "open", 50);
                }
            }
        }

        private float DetermineHeuristicStrength(Pawn pawn, IntVec3 start, LocalTargetInfo dest)
        {
            if(!PawnIsShip(pawn))
            {
                Log.Error("Called method for pawn " + pawn + " in ShipPathFinder which should never occur. Contact mod author.");
            }

            float lengthHorizontal = (start - dest.Cell).LengthHorizontal;
            return (float)Mathf.RoundToInt(ShipPathFinder.NonRegionBasedHeuristicStrengthHuman_DistanceCurve.Evaluate(lengthHorizontal));
        }

        private Area GetAllowedArea(Pawn pawn)
        {
            if(!(pawn is null) && !(pawn.playerSettings is null) && !pawn.Drafted && ForbidUtility.CaresAboutForbidden(pawn, true))
            {
                Area area = pawn.playerSettings.EffectiveAreaRestrictionInPawnCurrentMap;
                if(!(area is null) && area.TrueCount <= 0)
                {
                    area = null;
                }
                return area;
            }
            return null;
        }

        private CellRect CalculateDestinationRect(LocalTargetInfo dest, PathEndMode peMode)
        {
            CellRect result;
            result = (!dest.HasThing || peMode == PathEndMode.OnCell) ? CellRect.SingleCell(dest.Cell) : dest.Thing.OccupiedRect();
            result = (peMode == PathEndMode.Touch) ? result.ExpandedBy(1) : result;
            return result;
        }

        private void CalculateAndAddDisallowedCorners(TraverseParms traverseParms, PathEndMode peMode, CellRect destinationRect)
        {
            this.disallowedCornerIndices.Clear();
            if (peMode == PathEndMode.Touch)
            {
                int minX = destinationRect.minX;
                int minZ = destinationRect.minZ;
                int maxX = destinationRect.maxX;
                int maxZ = destinationRect.maxZ;
                if (!this.IsCornerTouchAllowed(minX + 1, minZ + 1, minX + 1, minZ, minX, minZ + 1))
                {
                    this.disallowedCornerIndices.Add(this.map.cellIndices.CellToIndex(minX, minZ));
                }
                if (!this.IsCornerTouchAllowed(minX + 1, maxZ - 1, minX + 1, maxZ, minX, maxZ - 1))
                {
                    this.disallowedCornerIndices.Add(this.map.cellIndices.CellToIndex(minX, maxZ));
                }
                if (!this.IsCornerTouchAllowed(maxX - 1, maxZ - 1, maxX - 1, maxZ, maxX, maxZ - 1))
                {
                    this.disallowedCornerIndices.Add(this.map.cellIndices.CellToIndex(maxX, maxZ));
                }
                if (!this.IsCornerTouchAllowed(maxX - 1, minZ + 1, maxX - 1, minZ, maxX, minZ + 1))
                {
                    this.disallowedCornerIndices.Add(this.map.cellIndices.CellToIndex(maxX, minZ));
                }
            }
        }

        private bool IsCornerTouchAllowed(int cornerX, int cornerZ, int adjCardinal1X, int adjCardinal1Z, int adjCardinal2X, int adjCardinal2Z)
        {
            return TouchPathEndModeUtility.IsCornerTouchAllowed(cornerX, cornerZ, adjCardinal1X, adjCardinal1Z, adjCardinal2X, adjCardinal2Z, this.map);
        }
        

        private Map map;

        private MapExtension mapE;

        private FastPriorityQueue<ShipPathFinder.CostNode> openList;

        private ShipPathFinder.ShipPathFinderNodeFast[] calcGrid;

        private ushort statusOpenValue = 1;

        private ushort statusClosedValue = 2;

        private RegionCostCalculatorWrapperShips regionCostCalculator;

        private int mapSizeX;

        private int mapSizeZ;

        private ShipPathGrid shipPathGrid;

        private Building[] edificeGrid;

        private List<Blueprint>[] blueprintGrid;

        private CellIndices cellIndices;

        private List<int> disallowedCornerIndices = new List<int>(4);

        public const int DefaultMoveTicksCardinal = 13;

        private const int DefaultMoveTicksDiagonal = 18;

        private const int SearchLimit = 160000;

        private static readonly int[] Directions = new int[]
        {
            0,
            1,
            0,
            -1,
            1,
            1,
            -1,
            -1,
            -1,
            0,
            1,
            0,
            -1,
            1,
            1,
            -1
        };

        //costdoortobash
        //costblockedwallbase
        //costblockedwallextraperhitpoint
        //costblockeddoor
        //costblockeddoorperhitpoint

        public const int Cost_OutsideAllowedArea = 600;

        private const int Cost_PawnCollision = 200; //175
        
        private const int NodesToOpenBeforeRegionbasedPathing_NonShip = 2000; //Needed?

        private const int NodesToOpenBeforeRegionBasedPathing_Ship = 100000; //Needed

        private static readonly SimpleCurve NonRegionBasedHeuristicStrengthHuman_DistanceCurve = new SimpleCurve
        {
            {
                new CurvePoint(40f, 1f),
                true
            },
            {
                new CurvePoint(120f, 2.8f),
                true
            }
        };

        private static readonly SimpleCurve RegionheuristicWeighByNodesOpened = new SimpleCurve
        {
            {
                new CurvePoint(0f, 1f),
                true
            },
            {
                new CurvePoint(3500f, 1f),
                true
            },
            {
                new CurvePoint(4500f, 5f),
                true
            },
            {
                new CurvePoint(30000f, 50f),
                true
            },
            {
                new CurvePoint(100000f, 500f),
                true
            }
        };

        internal struct CostNode
        {
            public CostNode(int index, int cost)
            {
                this.index = index;
                this.cost = cost;
            }
            public int index;
            public int cost;
        }

        private struct ShipPathFinderNodeFast
        {
            public int knownCost;
            public int heuristicCost;
            public int parentIndex;
            public int costNodeCost;
            public ushort status;
        }

        internal class CostNodeComparer : IComparer<ShipPathFinder.CostNode>
        {
            public int Compare(ShipPathFinder.CostNode a, ShipPathFinder.CostNode b)
            {
                return a.cost.CompareTo(b.cost);
            }
        }
    }
}
