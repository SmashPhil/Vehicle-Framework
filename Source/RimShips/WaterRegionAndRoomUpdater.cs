using System;
using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimShips.AI;
using Verse;

namespace RimShips
{
    public class WaterRegionAndRoomUpdater
    {
        public WaterRegionAndRoomUpdater(Map map)
        {
            this.map = map;
        }

        public bool Enabled
        {
            get
            {
                return this.enabledInt;
            }
            set
            {
                this.enabledInt = value;
            }
        }

        public bool AnythingToRebuild => !this.initialized;

        public void RebuildAllWaterRegions()
        {
            if (!this.Enabled)
                Log.Warning("Called RebuildAllRegions but WaterRegionAndRoomUpdater is disabled. Regions won't be rebuilt.", false);

            AccessTools.Method(type: typeof(RegionDirtyer), name: "SetAllDirty").Invoke(this.map.regionDirtyer, null);
            this.TryRebuildWaterRegions();
        }

        public void TryRebuildWaterRegions()
        {
            if (this.working || !this.Enabled)
                return;
            this.working = true;
            if (!this.initialized)
                this.RebuildAllWaterRegions();
            if(!this.map.regionDirtyer.AnyDirty)
            {
                this.working = false;
                return;
            }
            try
            {
                this.RegenerateNewWaterRegions();
                this.CreateOrUpdateWaterRooms();
            }
            catch(Exception exc)
            {
                Log.Error("Exception while rebuilding water regions: " + exc, false);
            }
            this.newRegions.Clear();
            this.initialized = true;
            this.working = false;
        }

        private void RegenerateNewWaterRegions()
        {
            this.newRegions.Clear();
            List<IntVec3> cells = this.map.regionDirtyer.DirtyCells;
            foreach(IntVec3 c  in cells)
            {
                if(WaterGridsUtility.GetRegion(c, this.map, RegionType.Set_All) is null)
                {
                    WaterRegion region = MapExtensionUtility.GetExtensionToMap(map).getWaterRegionmaker.TryGenerateRegionFrom(c);
                    
                    if (!(region is null))
                        this.newRegions.Add(region);
                }
            }
        }

        private void CreateOrUpdateWaterRooms()
        {
            this.newRooms.Clear();
            this.reusedOldRooms.Clear();
            /*int numRegionGroups = this.CombineNewRegionsIntoContiguousGroups();
            this.CreateOrAttackToExistingRooms(numRegionGroups);
            int numRoomGroups = this.CombineNewAndReusedRoomsIntoContiguousGroups();*/

            this.newRooms.Clear();
            this.reusedOldRooms.Clear();
        }

/*        private int CombineNewRegionsIntoContiguousGroups()
        {
            int num = 0;
            foreach(WaterRegion region in this.newRegions)
            {
                if(region.newRegionGroupIndex < 0)
                {
                    WaterRegionTraverser.FloodAndSetNewRegionIndex(region, num);
                    num++;
                }
            }
            return num;
        }
*/


        private Map map;

        private List<WaterRegion> newRegions = new List<WaterRegion>();

        private List<WaterRoom> newRooms = new List<WaterRoom>();

        private HashSet<WaterRoom> reusedOldRooms = new HashSet<WaterRoom>();

        private List<WaterRoom> currentRoomGroup = new List<WaterRoom>();

        private Stack<WaterRoom> tmpRoomStack = new Stack<WaterRoom>();

        private bool initialized;

        private bool working;

        private bool enabledInt = true;
    }
}
