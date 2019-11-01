using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimShips.Build;
using RimShips.Defs;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.AI
{
    public class RegionCostCalculatorWrapperShips
    {
        public RegionCostCalculatorWrapperShips(Map map)
        {
            this.map = map;
            this.regionCostCalculatorShips = new RegionCostCalculatorShips(map);
        }

        public void Init(CellRect end, TraverseParms traverseParms, int moveTicksCardinal, int moveTicksDiagonal, ByteGrid avoidGrid, Area allowedArea, bool drafted, List<int> disallowedCorners)
        {
            this.moveTicksCardinal = moveTicksCardinal;
            this.moveTicksDiagonal = moveTicksDiagonal;
            this.endCell = end.CenterCell;
            this.cachedRegion = null;
            this.cachedBestLink = null;
            this.cachedSecondBestLink = null;
            this.cachedBestLinkCost = 0;
            this.cachedSecondBestLinkCost = 0;
            this.cachedRegionIsDestination = false;
            this.regionGrid = MapExtensionUtility.GetExtensionToMap(this.map).getWaterRegionGrid.DirectGrid;
            this.destRegions.Clear();
            if (end.Width == 1 && end.Height == 1)
            {
                WaterRegion region = WaterGridsUtility.GetRegion(this.endCell, this.map, RegionType.Set_Passable);
                if (region != null)
                {
                    this.destRegions.Add(region);
                }
            }
            else
            {
                CellRect.CellRectIterator iterator = end.GetIterator();
                while (!iterator.Done())
                {
                    IntVec3 intVec = iterator.Current;
                    if (GenGridShips.InBounds(intVec, this.map) && !disallowedCorners.Contains(this.map.cellIndices.CellToIndex(intVec)))
                    {
                        WaterRegion region2 = WaterGridsUtility.GetRegion(intVec, this.map, RegionType.Set_Passable);
                        if (region2 != null)
                        {
                            if (region2.Allows(traverseParms, true))
                            {
                                this.destRegions.Add(region2);
                            }
                        }
                    }
                    iterator.MoveNext();
                }
            }
            if (this.destRegions.Count == 0)
            {
                Log.Error("Couldn't find any destination regions. This shouldn't ever happen because we've checked reachability.", false);
            }
            this.regionCostCalculatorShips.Init(end, this.destRegions, traverseParms, moveTicksCardinal, moveTicksDiagonal, avoidGrid, allowedArea, drafted);
        }

        public int GetPathCostFromDestToRegion(int cellIndex)
        {
            WaterRegion region = this.regionGrid[cellIndex];
            IntVec3 cell = this.map.cellIndices.IndexToCell(cellIndex);
            if (region != this.cachedRegion)
            {
                this.cachedRegionIsDestination = this.destRegions.Contains(region);
                if (this.cachedRegionIsDestination)
                {
                    return this.OctileDistanceToEnd(cell);
                }
                this.cachedBestLinkCost = this.regionCostCalculatorShips.GetRegionBestDistances(region, out this.cachedBestLink, out this.cachedSecondBestLink, out this.cachedSecondBestLinkCost);
                this.cachedRegion = region;
            }
            else if (this.cachedRegionIsDestination)
            {
                return this.OctileDistanceToEnd(cell);
            }
            if (this.cachedBestLink != null)
            {
                int num = this.regionCostCalculatorShips.RegionLinkDistance(cell, this.cachedBestLink, 1);
                int num3;
                if (this.cachedSecondBestLink != null)
                {
                    int num2 = this.regionCostCalculatorShips.RegionLinkDistance(cell, this.cachedSecondBestLink, 1);
                    num3 = Mathf.Min(this.cachedSecondBestLinkCost + num2, this.cachedBestLinkCost + num);
                }
                else
                {
                    num3 = this.cachedBestLinkCost + num;
                }
                return num3 + this.OctileDistanceToEndEps(cell);
            }
            return 10000;
        }

        private int OctileDistanceToEnd(IntVec3 cell)
        {
            int dx = Mathf.Abs(cell.x - this.endCell.x);
            int dz = Mathf.Abs(cell.z - this.endCell.z);
            return GenMath.OctileDistance(dx, dz, this.moveTicksCardinal, this.moveTicksDiagonal);
        }

        private int OctileDistanceToEndEps(IntVec3 cell)
        {
            int dx = Mathf.Abs(cell.x - this.endCell.x);
            int dz = Mathf.Abs(cell.z - this.endCell.z);
            return GenMath.OctileDistance(dx, dz, 2, 3);
        }

        private Map map;

        private IntVec3 endCell;

        private HashSet<WaterRegion> destRegions = new HashSet<WaterRegion>();

        private int moveTicksCardinal;

        private int moveTicksDiagonal;

        private RegionCostCalculatorShips regionCostCalculatorShips;

        private WaterRegion cachedRegion;

        private WaterRegionLink cachedBestLink;

        private WaterRegionLink cachedSecondBestLink;

        private int cachedBestLinkCost;

        private int cachedSecondBestLinkCost;

        private bool cachedRegionIsDestination;

        private WaterRegion[] regionGrid;
    }
}
