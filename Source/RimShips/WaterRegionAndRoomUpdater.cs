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

        public void RebuildAllWaterRegionsAndRooms()
        {
            if (!this.Enabled)
                Log.Warning("Called RebuildAllWaterRegionAndRooms() but WaterRegionAndRoomUpdater is disabled. Water Regions won't be rebuilt.", false);
            this.map.temperatureCache.ResetTemperatureCache();
        }
        
        //CONTINUE HERE

        private Map map;

        private List<WaterRegion> newRegions = new List<WaterRegion>();

        private List<WaterRegion> currentRegionGroup = new List<WaterRegion>();
        private bool initialized;

        private bool working;

        private bool enabledInt = true;
    }
}
