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

namespace RimShips
{
    public delegate bool WaterRegionEntryPredicate(WaterRegion from, WaterRegion to);

    public delegate bool WaterRegionProcessor(WaterRegion reg);
    public static class WaterRegionTraverser
    {
        static WaterRegionTraverser()
        {
            WaterRegionTraverser.RecreateWorkers();
        }

        //FloodAndSetRooms

        //FloodAndSetNewRegionIndex

        public static bool WithinRegions(this IntVec3 A, IntVec3 B, Map map, int regionLookCount, TraverseParms traverseParams, RegionType traversableRegionTypes = RegionType.Set_Passable)
        {
            WaterRegion region = WaterGridsUtility.GetRegion(A, map, traversableRegionTypes);
            if (region is null)
                return false;
            WaterRegion regB = WaterGridsUtility.GetRegion(B, map, traversableRegionTypes);
            if (regB is null)
                return false;
            if (region == regB)
                return true;
            WaterRegionEntryPredicate entryCondition = (WaterRegion from, WaterRegion r) => r.Allows(traverseParams, false);
            bool found = false;
            WaterRegionProcessor regionProcessor = delegate (WaterRegion r)
            {
                if (r == regB)
                {
                    found = true;
                    return true;
                }
                return false;
            };
            WaterRegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, regionLookCount, traversableRegionTypes);
            return found;
        }

        public static void MarkRegionsBFS(WaterRegion root, WaterRegionEntryPredicate entryCondition, int maxRegions, int inRadiusMark, RegionType traversableRegionTypes = RegionType.Set_Passable)
        {
            WaterRegionTraverser.BreadthFirstTraverse(root, entryCondition, delegate (WaterRegion r)
            {
                r.mark = inRadiusMark;
                return false;
            }, maxRegions, traversableRegionTypes);
        }

        public static bool ShouldCountRegion(WaterRegion r)
        {
            return !r.IsDoorway;
        }

        public static void RecreateWorkers()
        {
            WaterRegionTraverser.freeWorkers.Clear();
            for (int i = 0; i < WaterRegionTraverser.NumWorkers; i++)
                WaterRegionTraverser.freeWorkers.Enqueue(new WaterRegionTraverser.BFSWorker(i));
        }

        public static void BreadthFirstTraverse(IntVec3 start, Map map, WaterRegionEntryPredicate entryCondition, WaterRegionProcessor regionProcessor, int maxRegions = 999999, RegionType traversableRegionTypes = RegionType.Set_Passable)
        {
            WaterRegion region = WaterGridsUtility.GetRegion(start, map, traversableRegionTypes);
            if (region is null)
                return;
            WaterRegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
        }

        public static void BreadthFirstTraverse(WaterRegion root, WaterRegionEntryPredicate entryCondition, WaterRegionProcessor regionProcessor, int maxRegions = 999999, RegionType traversableRegionTypes = RegionType.Set_Passable)
        {
            if(WaterRegionTraverser.freeWorkers.Count == 0)
            {
                Log.Error("No free workers for BFS. Either BFS recurred deeper than " + WaterRegionTraverser.NumWorkers + ", or a bug has put this system in an inconsistent state. Resetting.", false);
                return;
            }
            if(root is null)
            {
                Log.Error("BFS with null root region.", false);
                return;
            }
            WaterRegionTraverser.BFSWorker bfsworker = WaterRegionTraverser.freeWorkers.Dequeue();
            try
            {
                bfsworker.BreadthFirstTraverseWork(root, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
            }
            catch(Exception ex)
            {
                Log.Error("Exception in BreadthFirstTraverse: " + ex.ToString(), false);
            }
            finally
            {
                bfsworker.Clear();
                WaterRegionTraverser.freeWorkers.Enqueue(bfsworker);
            }
        }

        private static Queue<WaterRegionTraverser.BFSWorker> freeWorkers = new Queue<WaterRegionTraverser.BFSWorker>();

        public static int NumWorkers = 8;

        public static readonly WaterRegionEntryPredicate PassAll = (WaterRegion from, WaterRegion to) => true;

        private class BFSWorker
        {
            public BFSWorker(int closedArrayPos)
            {
                this.closedArrayPos = closedArrayPos;
            }

            public void Clear()
            {
                this.open.Clear();
            }

            private void QueueNewOpenRegion(WaterRegion region)
            {
                if (region.closedIndex[this.closedArrayPos] == this.closedIndex)
                {
                    throw new InvalidOperationException("Region is already closed; you can't open it. Region: " + region.ToString());
                }
                this.open.Enqueue(region);
                region.closedIndex[this.closedArrayPos] = this.closedIndex;
            }

            private void FinalizeSearch() { }

            public void BreadthFirstTraverseWork(WaterRegion root, WaterRegionEntryPredicate entryCondition, WaterRegionProcessor regionProcessor, int maxRegions, RegionType traversableRegionTypes)
            {
                if ((root.type & traversableRegionTypes) == RegionType.None)
                    return;
                this.closedIndex += 1u;
                this.open.Clear();
                this.numRegionsProcessed = 0;
                this.QueueNewOpenRegion(root);
                while(this.open.Count > 0)
                {
                    WaterRegion region = this.open.Dequeue();
                    if(ShipHarmony.debug)
                    {
                        region.Debug_Notify_Traversed();
                    }
                    if(!(regionProcessor is null) && regionProcessor(region))
                    {
                        this.FinalizeSearch();
                        return;
                    }
                    if (WaterRegionTraverser.ShouldCountRegion(region))
                        this.numRegionsProcessed++;
                    if(this.numRegionsProcessed >= maxRegions)
                    {
                        this.FinalizeSearch();
                        return;
                    }
                    for(int i = 0; i < region.links.Count; i++)
                    {
                        WaterRegionLink regionLink = region.links[i];
                        for(int j = 0; j < 2; j++)
                        {
                            WaterRegion region2 = regionLink.regions[j];
                            if(!(region2 is null) && region2.closedIndex[this.closedArrayPos] != this.closedIndex && (region2.type & traversableRegionTypes) != RegionType.None &&
                                (entryCondition is null || entryCondition(region, region2)))
                            {
                                this.QueueNewOpenRegion(region2);
                            }
                        }
                    }
                }
                this.FinalizeSearch();
            }

            private Queue<WaterRegion> open = new Queue<WaterRegion>();

            private int numRegionsProcessed;

            private uint closedIndex = 1u;

            private int closedArrayPos;

            private const int skippableRegionSize = 4;
        }
    }
}
