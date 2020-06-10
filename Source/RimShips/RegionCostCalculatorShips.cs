using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Vehicles.AI
{
    public class RegionCostCalculatorShips
    {
        public RegionCostCalculatorShips(Map map)
        {
            this.map = map;
            this.mapE = MapExtensionUtility.GetExtensionToMap(map);
            this.preciseRegionLinkDistancesDistanceGetter = new Func<int, int, float>(this.PreciseRegionLinkDistancesDistanceGetter);
        }

        public void Init(CellRect destination, HashSet<WaterRegion> destRegions, TraverseParms parms, int moveTicksCardinal, int moveTicksDiagonal, ByteGrid avoidGrid, Area allowedArea, bool drafted)
        {
            this.regionGrid = this.mapE.getWaterRegionGrid.DirectGrid;
            this.traverseParms = parms;
            this.destinationCell = destination.CenterCell;
            this.moveTicksCardinal = moveTicksCardinal;
            this.moveTicksDiagonal = moveTicksDiagonal;
            this.avoidGrid = avoidGrid;
            this.allowedArea = allowedArea;
            this.drafted = drafted;
            this.regionMinLink.Clear();
            this.distances.Clear();
            this.linkTargetCells.Clear();
            this.queue.Clear();
            this.minPathCosts.Clear();

            foreach(WaterRegion region in destRegions)
            {
                int minPathCost = this.RegionMedianPathCost(region);
                foreach(WaterRegionLink regionLink in region.links)
                {
                    if (regionLink.GetOtherRegion(region).Allows(this.traverseParms, false))
                    {
                        int num = this.RegionLinkDistance(this.destinationCell, regionLink, minPathCost);
                        int num2;
                        if(this.distances.TryGetValue(regionLink, out num2))
                        {
                            if(num < num2)
                            {
                                this.linkTargetCells[regionLink] = this.GetLinkTargetCell(this.destinationCell, regionLink);
                            }
                            num = Math.Min(num2, num);
                        }
                        else
                        {
                            this.linkTargetCells[regionLink] = this.GetLinkTargetCell(this.destinationCell, regionLink);
                        }
                        this.distances[regionLink] = num;
                    }
                }
                this.GetPreciseRegionLinkDistances(region, destination, this.preciseRegionLinkDistances);
                for(int i = 0; i < this.preciseRegionLinkDistances.Count; i++)
                {
                    Pair<WaterRegionLink, int> pair = this.preciseRegionLinkDistances[i];
                    WaterRegionLink first = pair.First;
                    int num3 = this.distances[first];
                    int num4;
                    if(pair.Second > num3)
                    {
                        this.distances[first] = pair.Second;
                        num4 = pair.Second;
                    }
                    else
                    {
                        num4 = num3;
                    }
                    this.queue.Push(new RegionCostCalculatorShips.RegionLinkQueueEntry(region, first, num4, num4));
                }
            }
        }

        public int GetRegionDistance(WaterRegion region, out WaterRegionLink minLink)
        {
            if(this.regionMinLink.TryGetValue(region.id, out minLink))
                return this.distances[minLink];
            while(this.queue.Count != 0)
            {
                RegionCostCalculatorShips.RegionLinkQueueEntry regionLinkQueueEntry = this.queue.Pop();
                int num = this.distances[regionLinkQueueEntry.Link];
                if(regionLinkQueueEntry.Cost == num)
                {
                    WaterRegion otherRegion = regionLinkQueueEntry.Link.GetOtherRegion(regionLinkQueueEntry.From);
                    if(!(otherRegion is null) && otherRegion.valid)
                    {
                        int num2 = 0;
                        if(!(otherRegion.door is null))
                        {
                            num2 = VehiclePathFinder.GetBuildingCost(otherRegion.door, this.traverseParms, this.traverseParms.pawn);
                            if (num2 == int.MaxValue) continue;
                            num2 += this.OctileDistance(1, 0);
                        }
                        int minPathCost = this.RegionMedianPathCost(otherRegion);
                        foreach(WaterRegionLink regionLink in otherRegion.links)
                        {
                            if(regionLink != regionLinkQueueEntry.Link && regionLink.GetOtherRegion(otherRegion).type.Passable())
                            {
                                int num3 = (otherRegion.door is null) ? this.RegionLinkDistance(regionLinkQueueEntry.Link, regionLink, minPathCost) : num2;
                                num3 = Math.Max(num3, 1);
                                int num4 = num + num3;
                                int estimatedPathCost = this.MinimumRegionLinkDistance(this.destinationCell, regionLink) + num4;
                                int num5;
                                if(this.distances.TryGetValue(regionLink, out num5))
                                {
                                    if(num4 < num5)
                                    {
                                        this.distances[regionLink] = num4;
                                        this.queue.Push(new RegionCostCalculatorShips.RegionLinkQueueEntry(otherRegion, regionLink, num4, estimatedPathCost));
                                    }
                                }
                                else
                                {
                                    this.distances.Add(regionLink, num4);
                                    this.queue.Push(new RegionCostCalculatorShips.RegionLinkQueueEntry(otherRegion, regionLink, num4, estimatedPathCost));
                                }
                            }
                        }
                        if(!this.regionMinLink.ContainsKey(otherRegion.id))
                        {
                            this.regionMinLink.Add(otherRegion.id, regionLinkQueueEntry.Link);
                            if(otherRegion == region)
                            {
                                minLink = regionLinkQueueEntry.Link;
                                return regionLinkQueueEntry.Cost;
                            }
                        }
                    }
                }
            }
            return 10000;
        }

        public int GetRegionBestDistances(WaterRegion region, out WaterRegionLink bestLink, out WaterRegionLink secondBestLink, out int secondBestCost)
        {
            int regionDistance = this.GetRegionDistance(region, out bestLink);
            secondBestLink = null;
            secondBestCost = int.MaxValue;
            foreach(WaterRegionLink regionLink in region.links)
            {
                if(regionLink != bestLink && regionLink.GetOtherRegion(region).type.Passable())
                {
                    int num;
                    if (this.distances.TryGetValue(regionLink, out num) && num < secondBestCost)
                    {
                        secondBestCost = num;
                        secondBestLink = regionLink;
                    }
                }
            }
            return regionDistance;
        }

        public int RegionMedianPathCost(WaterRegion region)
        {
            int result;
            if(this.minPathCosts.TryGetValue(region, out result))
            {
                return result;
            }
            bool ignoreAllowedAreaCost = this.allowedArea != null && region.OverlapWith(this.allowedArea) != AreaOverlap.None;
            CellIndices cellIndices = this.map.cellIndices;
            Rand.PushState();
            Rand.Seed = cellIndices.CellToIndex(region.extentsClose.CenterCell) * (region.links.Count + 1);
            for(int i = 0; i < SampleCount; i++)
            {
                RegionCostCalculatorShips.pathCostSamples[i] = this.GetCellCostFast(cellIndices.CellToIndex(region.RandomCell), ignoreAllowedAreaCost);
            }
            Rand.PopState();
            Array.Sort<int>(RegionCostCalculatorShips.pathCostSamples);
            int num = RegionCostCalculatorShips.pathCostSamples[4];
            this.minPathCosts[region] = num;
            return num;
        }

        private int GetCellCostFast(int index, bool ignoreAllowedAreaCost = false)
        {
            int num = mapE.getShipPathGrid.pathGrid[index];
            num += !(this.avoidGrid is null) ? (int)(this.avoidGrid[index] * 8) : 0;
            num += (!(this.allowedArea is null) && !ignoreAllowedAreaCost && !this.allowedArea[index]) ? 600 : 0;
            num += this.drafted ? this.map.terrainGrid.topGrid[index].extraDraftedPerceivedPathCost : this.map.terrainGrid.topGrid[index].extraNonDraftedPerceivedPathCost;
            return num;
        }

        private int RegionLinkDistance(WaterRegionLink a, WaterRegionLink b, int minPathCost)
        {
            IntVec3 a2 = (!this.linkTargetCells.ContainsKey(a)) ? RegionCostCalculatorShips.RegionLinkCenter(a) : this.linkTargetCells[a];
            IntVec3 b2 = (!this.linkTargetCells.ContainsKey(b)) ? RegionCostCalculatorShips.RegionLinkCenter(b) : this.linkTargetCells[b];
            IntVec3 intVec = a2 - b2;
            int num = Math.Abs(intVec.x);
            int num2 = Math.Abs(intVec.z);
            return this.OctileDistance(num, num2) + (minPathCost * Math.Max(num, num2)) + (minPathCost * Math.Min(num, num2));
        }

        public int RegionLinkDistance(IntVec3 cell, WaterRegionLink link, int minPathCost)
        {
            IntVec3 linkTargetCell = this.GetLinkTargetCell(cell, link);
            IntVec3 intVec = cell - linkTargetCell;
            int num = Math.Abs(intVec.x);
            int num2 = Math.Abs(intVec.z);
            return this.OctileDistance(num, num2) + (minPathCost * Math.Max(num, num2)) + (minPathCost * Math.Min(num, num2));
        }

        private static int SpanCenterX(EdgeSpan e)
        {
            return e.root.x + ((e.dir != SpanDirection.East) ? 0 : (e.length / 2));
        }

        private static int SpanCenterZ(EdgeSpan e)
        {
            return e.root.z + ((e.dir != SpanDirection.North) ? 0 : (e.length / 2));
        }

        private static IntVec3 RegionLinkCenter(WaterRegionLink link)
        {
            return new IntVec3(RegionCostCalculatorShips.SpanCenterX(link.span), 0, RegionCostCalculatorShips.SpanCenterZ(link.span));
        }

        private int MinimumRegionLinkDistance(IntVec3 cell, WaterRegionLink link)
        {
            IntVec3 intVec = cell - RegionCostCalculatorShips.LinkClosestCell(cell, link);
            return this.OctileDistance(Math.Abs(intVec.x), Math.Abs(intVec.z));
        }

        private int OctileDistance(int dx, int dz)
        {
            return GenMath.OctileDistance(dx, dz, this.moveTicksCardinal, this.moveTicksDiagonal);
        }

        private IntVec3 GetLinkTargetCell(IntVec3 cell,  WaterRegionLink link)
        {
            return RegionCostCalculatorShips.LinkClosestCell(cell, link);
        }

        private static IntVec3 LinkClosestCell(IntVec3 cell, WaterRegionLink link)
        {
            EdgeSpan span = link.span;
            int num = 0;
            int num2 = 0;
            if(span.dir == SpanDirection.North)
            {
                num2 = span.length - 1;
            }
            else
            {
                num = span.length - 1;
            }

            IntVec3 root = span.root;
            return new IntVec3(Mathf.Clamp(cell.x, root.x, root.x + num), 0, Mathf.Clamp(cell.z, root.z, root.z + num2));
        }

        private void GetPreciseRegionLinkDistances(WaterRegion region, CellRect destination, List<Pair<WaterRegionLink, int>> outDistances)
        {
            outDistances.Clear();
            RegionCostCalculatorShips.tmpCellIndices.Clear();
            if(destination.Width == 1 && destination.Height == 1)
            {
                RegionCostCalculatorShips.tmpCellIndices.Add(this.map.cellIndices.CellToIndex(destination.CenterCell));
            }
            else
            {
                CellRect.CellRectIterator iterator = destination.GetIterator();
                while(!iterator.Done())
                {
                    IntVec3 c = iterator.Current;
                    if(c.InBoundsShip(this.map))
                    {
                        RegionCostCalculatorShips.tmpCellIndices.Add(this.map.cellIndices.CellToIndex(c));
                    }
                    iterator.MoveNext();
                }
            }
            Dijkstra<int>.Run(RegionCostCalculatorShips.tmpCellIndices, (int x) => this.PreciseRegionLinkDistancesNeighborsGetter(x, region),
                this.preciseRegionLinkDistancesDistanceGetter, RegionCostCalculatorShips.tmpDistances, null);
            foreach(WaterRegionLink regionLink in region.links)
            {
                if(regionLink.GetOtherRegion(region).Allows(this.traverseParms, false))
                {
                    float num;
                    if(!RegionCostCalculatorShips.tmpDistances.TryGetValue(this.map.cellIndices.CellToIndex(this.linkTargetCells[regionLink]), out num))
                    {
                        Log.ErrorOnce("Dijkstra couldn't reach one of the cells even though they are in the same region. There is most likely something wrong with the " +
                            "neighbor nodes getter. Error occurred in ShipPathFinder of Vehicles", 1938471531, false);
                        num = 100f;
                    }
                    outDistances.Add(new Pair<WaterRegionLink, int>(regionLink, (int)num));
                }
            }
        }

        private IEnumerable<int> PreciseRegionLinkDistancesNeighborsGetter(int node, WaterRegion region)
        {
            if (this.regionGrid[node] is null || this.regionGrid[node] != region)
                return null;
            return this.PathableNeighborIndices(node);
        }

        private float PreciseRegionLinkDistancesDistanceGetter(int a, int b)
        {
            return (float)(this.GetCellCostFast(b, false) + ((!this.AreCellsDiagonal(a, b)) ? this.moveTicksCardinal : this.moveTicksDiagonal));
        }

        private bool AreCellsDiagonal(int a, int b)
        {
            int x = this.map.Size.x;
            return a % x != b % x && a / x != b / x;
        }

        private List<int> PathableNeighborIndices(int index)
        {
            RegionCostCalculatorShips.tmpPathableNeighborIndices.Clear();
            ShipPathGrid pathGrid = mapE.getShipPathGrid;
            int x = this.map.Size.x;
            bool flag = index % x > 0;
            bool flag2 = index % x < x - 1;
            bool flag3 = index >= x;
            bool flag4 = index / x < this.map.Size.z - 1;
            if(flag3 && pathGrid.WalkableFast(index - x))
            {
                RegionCostCalculatorShips.tmpPathableNeighborIndices.Add(index - x);
            }
            if(flag2 && pathGrid.WalkableFast(index + 1))
            {
                RegionCostCalculatorShips.tmpPathableNeighborIndices.Add(index + 1);
            }
            if(flag && pathGrid.WalkableFast(index - 1))
            {
                RegionCostCalculatorShips.tmpPathableNeighborIndices.Add(index - 1);
            }
            if(flag4 && pathGrid.WalkableFast(index + x))
            {
                RegionCostCalculatorShips.tmpPathableNeighborIndices.Add(index + x);
            }
            bool flag5 = !flag || VehiclePathFinder.BlocksDiagonalMovement(index - 1, map);
            bool flag6 = !flag2 || VehiclePathFinder.BlocksDiagonalMovement(index + 1, map);
            if(flag3 && !VehiclePathFinder.BlocksDiagonalMovement(index - x, map))
            {
                if(!flag6 && pathGrid.WalkableFast(index - x + 1))
                {
                    RegionCostCalculatorShips.tmpPathableNeighborIndices.Add(index - x + 1);
                }
                if(!flag5 && pathGrid.WalkableFast(index - x - 1))
                {
                    RegionCostCalculatorShips.tmpPathableNeighborIndices.Add(index - x - 1);
                }
            }
            if(flag4 && !VehiclePathFinder.BlocksDiagonalMovement(index + x, map))
            {
                if(!flag6 && pathGrid.WalkableFast(index + x + 1))
                {
                    RegionCostCalculatorShips.tmpPathableNeighborIndices.Add(index + x + 1);
                }
                if(!flag5 && pathGrid.WalkableFast(index + x - 1))
                {
                    RegionCostCalculatorShips.tmpPathableNeighborIndices.Add(index + x - 1);
                }
            }
            return RegionCostCalculatorShips.tmpPathableNeighborIndices;
        }

        private Map map;

        private MapExtension mapE;

        private WaterRegion[] regionGrid;

        private TraverseParms traverseParms;

        private IntVec3 destinationCell;

        private int moveTicksCardinal;

        private int moveTicksDiagonal;

        private ByteGrid avoidGrid;

        private Area allowedArea;

        private bool drafted;

        private Func<int, int, float> preciseRegionLinkDistancesDistanceGetter;

        private Dictionary<int, WaterRegionLink> regionMinLink = new Dictionary<int, WaterRegionLink>();

        private Dictionary<WaterRegionLink, int> distances = new Dictionary<WaterRegionLink, int>();

        private FastPriorityQueue<RegionCostCalculatorShips.RegionLinkQueueEntry> queue = new FastPriorityQueue<RegionCostCalculatorShips.RegionLinkQueueEntry>(new RegionCostCalculatorShips.DistanceComparer());

        private Dictionary<WaterRegion, int> minPathCosts = new Dictionary<WaterRegion, int>();

        private List<Pair<WaterRegionLink, int>> preciseRegionLinkDistances = new List<Pair<WaterRegionLink, int>>();

        private Dictionary<WaterRegionLink, IntVec3> linkTargetCells = new Dictionary<WaterRegionLink, IntVec3>();

        private const int SampleCount = 11;

        private static int[] pathCostSamples = new int[SampleCount];

        private static List<int> tmpCellIndices = new List<int>();

        private static Dictionary<int, float> tmpDistances = new Dictionary<int, float>();

        private static List<int> tmpPathableNeighborIndices = new List<int>();

        private struct RegionLinkQueueEntry
        {
            public RegionLinkQueueEntry(WaterRegion from, WaterRegionLink link, int cost, int estimatedPathCost)
            {
                this.from = from;
                this.link = link;
                this.cost = cost;
                this.estimatedPathCost = estimatedPathCost;
            }
            public WaterRegion From
            {
                get
                {
                    return this.from;
                }
            }

            public WaterRegionLink Link
            {
                get
                {
                    return this.link;
                }
            }

            public int Cost
            {
                get
                {
                    return this.cost;
                }
            }

            public int EstimatedPathCost
            {
                get
                {
                    return this.estimatedPathCost;
                }
            }

            private WaterRegion from;

            private WaterRegionLink link;

            private int cost;

            private int estimatedPathCost;
        }

        private class DistanceComparer : IComparer<RegionCostCalculatorShips.RegionLinkQueueEntry>
        {
            public int Compare(RegionCostCalculatorShips.RegionLinkQueueEntry a, RegionCostCalculatorShips.RegionLinkQueueEntry b)
            {
                return a.EstimatedPathCost.CompareTo(b.EstimatedPathCost);
            }
        }
    }
}
