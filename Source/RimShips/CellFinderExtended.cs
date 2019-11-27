using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using RimShips.AI;

namespace RimShips
{
    public static class CellFinderExtended
    {
        public static IntVec3 RandomEdgeCell(Rot4 dir, Map map, Predicate<IntVec3> validator)
        {
            List<IntVec3> cellsToCheck = CellRect.WholeMap(map).GetEdgeCells(dir).ToList();
            for(;;)
            {
                IntVec3 rCell = SPExtended.PopRandom(ref cellsToCheck);
                if(validator(rCell))
                    return rCell;
                if(cellsToCheck.Count <= 0)
                {
                    Log.Warning("Failed to find edge cell at " + dir);
                    break;
                }
            }
            return CellFinder.RandomEdgeCell(map);
        }

        public static IntVec3 MiddleEdgeCell(Rot4 dir, Map map, Pawn pawn, Predicate<IntVec3> validator)
        {
            List<IntVec3> cellsToCheck = CellRect.WholeMap(map).GetEdgeCells(dir).ToList();
            
            int startIndex = cellsToCheck.Count / 2;
            int i = 0;
            while(cellsToCheck.Count > 0 && i < cellsToCheck.Count/2)
            {
                if(i > cellsToCheck.Count)
                {
                    Log.Warning("List of Cells almost went out of bounds. Report to Boats mod author - Smash Phil");
                    return cellsToCheck.Pop();
                }
                IntVec3 rCell = pawn.ClampToMap(cellsToCheck[startIndex + i], map, 2);
                if (ShipHarmony.debug) Log.Message("Checking r: " + rCell + " | " + validator(rCell));
                List<IntVec3> occupiedCellsRCell = pawn.PawnOccupiedCells(rCell);
                foreach(IntVec3 c in occupiedCellsRCell)
                {
                    if(!validator(c))
                        goto Block_0;
                }
                return rCell;

                Block_0:;
                IntVec3 lCell = pawn.ClampToMap(cellsToCheck[startIndex - i], map, 2);
                if (ShipHarmony.debug) Log.Message("Checking l: " + lCell + " | " + validator(lCell));
                List<IntVec3> occupiedCellsLCell = pawn.PawnOccupiedCells(rCell);
                foreach (IntVec3 c in occupiedCellsLCell)
                {
                    if (!validator(c))
                        goto Block_1;
                }
                return lCell;

                Block_1:;
                i++;
                if (ShipHarmony.debug) Log.Message("==============");
            }
            Log.Error("No edge cells spawned...? Problem with Boats by Smash Phil. Choosing Random Cell");
            return CellFinder.RandomCell(map);
        }

        public static bool TryFindRandomReachableCellNear(IntVec3 root, Map map, float radius, TraverseParms traverseParms, Predicate<IntVec3> validator, out IntVec3 result,
            Predicate<WaterRegion> regionValidator, int maxRegions = 999999)
        {
            if(map is null)
            {
                Log.ErrorOnce("Tried to find reachable cell using SPExtended in a null map", 61037855, false);
                result = IntVec3.Invalid;
                return false;
            }
            WaterRegion region = WaterGridsUtility.GetRegion(root, map, RegionType.Set_Passable);
            if(region is null)
            {
                result = IntVec3.Invalid;
                return false;
            }
            /* Temporary Middle Piece */
            result = CellFinderExtended.RandomEdgeCell(Find.World.CoastDirectionAt(map.Tile), map, (IntVec3 c) => GenGridShips.Standable(c, map, MapExtensionUtility.GetExtensionToMap(map)) && !c.Fogged(map));
            return true;
            /*                        */
            List<WaterRegion> workingRegions = new List<WaterRegion>();
            float radSquared = radius * radius;
            WaterRegionTraverser.BreadthFirstTraverse(region, (WaterRegion from, WaterRegion r) => r.Allows(traverseParms, true) && (radius > 1000f || r.extentsClose.ClosestDistSquaredTo(root) <=
            radSquared) && (regionValidator is null || regionValidator(r)), delegate (WaterRegion r)
            {
                workingRegions.Add(r);
                return false;
            }, maxRegions, RegionType.Set_Passable);

            while(workingRegions.Count > 0)
            {
                WaterRegion region2 = workingRegions.RandomElementByWeight((WaterRegion r) => (float)r.CellCount);
                if(region2.TryFindRandomCellInWaterRegion((IntVec3 c) => (float)(c - root).LengthHorizontalSquared <= radSquared && (validator is null || validator(c)), out result))
                {
                    return true;
                }
                workingRegions.Remove(region2);
            }
            result = CellFinderExtended.RandomEdgeCell(Find.World.CoastDirectionAt(map.Tile), map, (IntVec3 c) => GenGridShips.Standable(c, map, MapExtensionUtility.GetExtensionToMap(map)) && !c.Fogged(map));
            return false;
        }

        public static bool TryFindRandomCellInWaterRegion(this WaterRegion reg, Predicate<IntVec3> validator, out IntVec3 result)
        {
            for(int i = 0; i < 10; i++)
            {
                result = reg.RandomCell;
                if(validator is null || validator(result))
                    return true;
            }
            List<IntVec3> workingCells = new List<IntVec3>(reg.Cells);
            workingCells.Shuffle<IntVec3>();
            foreach(IntVec3 c in workingCells)
            {
                result = c;
                if (validator is null || validator(result))
                    return true;
            }
            result = reg.RandomCell;
            return false;
        }

        public static IntVec3 RandomClosewalkCellNear(IntVec3 root, Map map, int radius, Predicate<IntVec3> validator = null)
        {
            if(CellFinderExtended.TryRandomClosewalkCellNear(root, map, radius, out IntVec3 result, validator))
            {
                return result;
            }
            return root;
        }
        
        public static bool TryRandomClosewalkCellNear(IntVec3 root, Map map, int radius, out IntVec3 result, Predicate<IntVec3> validator = null)
        {
            return CellFinderExtended.TryFindRandomReachableCellNear(root, map, (float)radius, TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false), validator, out result,
                null, 999999);
        }

        public static IntVec3 RandomSpawnCellForPawnNear(IntVec3 root, Map map, int firstTryWithRadius = 4)
        {
            MapExtension mapE = MapExtensionUtility.GetExtensionToMap(map);
            if (GenGridShips.Standable(root, map, mapE) && root.GetFirstPawn(map) is null)
            {
                return root;
            }
            IntVec3 result;
            int num = firstTryWithRadius;
            for (int i = 0; i < 3; i++)
            {
                if(CellFinderExtended.TryFindRandomReachableCellNear(root, map, (float)num, TraverseParms.For(TraverseMode.NoPassClosedDoors, Danger.Deadly, false), (IntVec3 c)
                    => GenGridShips.Standable(c, map, mapE) && (root.Fogged(map) || !c.Fogged(map)) && c.GetFirstPawn(map) is null, out result, null, 999999))
                {
                    return result;
                }
                num *= 2;
            }
            num = firstTryWithRadius + 1;
            
            while(!CellFinderExtended.TryRandomClosewalkCellNear(root, map, num, out result, null))
            {
                if(num > map.Size.x / 2 && num > map.Size.z / 2)
                {
                    return root;
                }
                num *= 2;
            }
            return result;
        }
    }
}
