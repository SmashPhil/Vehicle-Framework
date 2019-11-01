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
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;
using RimShips.AI;
using RimShips.Defs;
using RimShips.Build;
using RimShips.Jobs;
using RimShips.Lords;
using RimShips.UI;

namespace RimShips
{
    public sealed class WaterRegionGrid
    {
        public WaterRegionGrid(Map map)
        {
            this.map = map;
            this.regionGrid = new WaterRegion[map.cellIndices.NumGridCells];
        }

        public WaterRegion[] DirectGrid
        {
            get
            {
                return this.regionGrid;
            }
        }

        public IEnumerable<WaterRegion> AllRegions_NoRebuild_InvalidAllowed
        {
            get
            {
                WaterRegionGrid.allRegionsYielded.Clear();
                try
                {
                    int count = this.map.cellIndices.NumGridCells;
                    for(int i = 0; i < count; i++)
                    {
                        if(!(this.regionGrid[i] is null) && !WaterRegionGrid.allRegionsYielded.Contains(this.regionGrid[i]))
                        {
                            yield return this.regionGrid[i];
                            WaterRegionGrid.allRegionsYielded.Add(this.regionGrid[i]);
                        }
                    }
                }
                finally
                {
                    WaterRegionGrid.allRegionsYielded.Clear();
                }
                yield break;
            }
        }

        public IEnumerable<WaterRegion> AllRegions
        {
            get
            {
                WaterRegionGrid.allRegionsYielded.Clear();
                try
                {
                    int count = this.map.cellIndices.NumGridCells;
                    for(int i = 0; i < count; i++)
                    {
                        if(!(this.regionGrid[i] is null) && this.regionGrid[i].valid && !WaterRegionGrid.allRegionsYielded.Contains
                            (this.regionGrid[i]))
                        {
                            yield return this.regionGrid[i];
                            WaterRegionGrid.allRegionsYielded.Add(this.regionGrid[i]);
                        }
                    }
                }
                finally
                {
                    WaterRegionGrid.allRegionsYielded.Clear();
                }
                yield break;
            }
        }

        public WaterRegion GetValidRegionAt(IntVec3 c)
        {
            if(!GenGridShips.InBounds(c, this.map))
            {
                Log.Error("Tried to get valid water region out of bounds at " + c, false);
            }
            WaterRegion region = this.regionGrid[this.map.cellIndices.CellToIndex(c)];
            
            return !(region is null) && region.valid ? region : null;
        }

        public WaterRegion GetValidRegionAt_NoRebuild(IntVec3 c)
        {
            if(!GenGridShips.InBounds(c, this.map))
            {
                Log.Error("Tried to get valid region out of bounds at " + c, false);
            }
            WaterRegion region = this.regionGrid[this.map.cellIndices.CellToIndex(c)];
            return !(region is null) && region.valid ? region : null;
        }

        public WaterRegion GetRegionAt_NoRebuild_InvalidAllowed(IntVec3 c)
        {
            return this.regionGrid[this.map.cellIndices.CellToIndex(c)];
        }

        public void SetRegionAt(IntVec3 c, WaterRegion reg)
        {
            this.regionGrid[this.map.cellIndices.CellToIndex(c)] = reg;
        }

        public void UpdateClean()
        {
            for(int i = 0; i < 16; i++)
            {
                if(this.curCleanIndex >= this.regionGrid.Length)
                {
                    this.curCleanIndex = 0;
                }
                WaterRegion region = this.regionGrid[this.curCleanIndex];
                if(!(region is null) && !region.valid)
                {
                    this.regionGrid[this.curCleanIndex] = null;
                }
                this.curCleanIndex++;
            }
        }

        public void DebugDraw()
        {
            if(this.map != Find.CurrentMap)
            {
                return;
            }
            
            if(ShipHarmony.debug)
            {
                CellRect currentViewRect = Find.CameraDriver.CurrentViewRect;
                currentViewRect.ClipInsideMap(this.map);
                foreach(IntVec3 c in currentViewRect)
                {
                    WaterRegion validRegionAt = this.GetValidRegionAt(c);
                    if(!(validRegionAt is null) && !this.drawnRegions.Contains(validRegionAt))
                    {
                        validRegionAt.DebugDraw();
                        this.drawnRegions.Add(validRegionAt);
                    }
                }
                this.drawnRegions.Clear();
            }
            IntVec3 intVec = Verse.UI.MouseCell();
            if(GenGridShips.InBounds(intVec, this.map))
            {
                WaterRegion regionAt_NoRebuild_InvalidAllowed = this.GetRegionAt_NoRebuild_InvalidAllowed(intVec);
                if (!(regionAt_NoRebuild_InvalidAllowed is null))
                    regionAt_NoRebuild_InvalidAllowed.DebugDrawMouseover();
            }
        }

        private Map map;

        private WaterRegion[] regionGrid;

        private int curCleanIndex;

        public List<Room> allRooms = new List<Room>();

        public static HashSet<WaterRegion> allRegionsYielded = new HashSet<WaterRegion>();

        private const int CleanSquaresPerFrame = 16;

        public HashSet<WaterRegion> drawnRegions = new HashSet<WaterRegion>();
    }
}
