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
    public sealed class MapExtension : IExposable
    {
        public MapExtension(Map map)
        {
            this.map = map;
            this.extensionID = map.uniqueID;
            MapExtensionUtility.StoreMapExtension(this);
        }

        public int MapExtensionID => this.extensionID;

        public ShipPathGrid getShipPathGrid => shipPathGrid;

        public ShipPathFinder getShipPathFinder => shipPathFinder;

        public ShipReachability getShipReachability => shipReachability;

        public WaterRegionGrid getWaterRegionGrid => waterRegionGrid;

        public WaterRegionMaker getWaterRegionmaker => waterRegionMaker;

        public WaterRegionLinkDatabase getWaterRegionLinkDatabase => waterRegionLinkDatabase;

        public WaterRegionAndRoomUpdater getWaterRegionAndRoomUpdater => waterRegionAndRoomUpdater;

        public void ConstructComponents()
        {
            this.shipPathGrid = new ShipPathGrid(this.map);
            this.shipPathFinder = new ShipPathFinder(this.map);
            this.shipReachability = new ShipReachability(this.map);
            this.waterRegionGrid = new WaterRegionGrid(this.map);
            this.waterRegionMaker = new WaterRegionMaker(this.map);
            this.waterRegionAndRoomUpdater = new WaterRegionAndRoomUpdater(this.map);
            this.waterRegionLinkDatabase = new WaterRegionLinkDatabase();
        }

        public void ExposeData()
        {
            Scribe_Deep.Look<ShipPathGrid>(ref this.shipPathGrid, "shipPathGrid", this.map, false);
            Scribe_Deep.Look<ShipPathFinder>(ref this.shipPathFinder, "shipPathFinder", this.map, this, false);
            Scribe_Deep.Look<ShipReachability>(ref this.shipReachability, "shipReachability", this.map, this, false);
            Scribe_Deep.Look<WaterRegion>(ref this.waterRegion, "waterRegion", this.map, this, false);
            Scribe_Deep.Look<WaterRegionMaker>(ref this.waterRegionMaker, "waterRegionMaker", this.map, this, false);
            Scribe_Deep.Look<WaterRegionGrid>(ref this.waterRegionGrid, "waterRegionGrid", this.map, this, false);
            Scribe_Deep.Look<WaterRegionLinkDatabase>(ref this.waterRegionLinkDatabase, "waterRegionLinkDatabase", false);
            Scribe_Values.Look<int>(ref this.extensionID, "extensionID", -1, false);
            if(Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.ConstructComponents();
            }
        }

        private ShipPathGrid shipPathGrid;

        private ShipPathFinder shipPathFinder;

        private ShipReachability shipReachability;

        private WaterRegion waterRegion;

        private WaterRegionMaker waterRegionMaker;

        private WaterRegionGrid waterRegionGrid;

        private WaterRegionLinkDatabase waterRegionLinkDatabase;

        private WaterRegionAndRoomUpdater waterRegionAndRoomUpdater;

        private readonly Map map;

        private int extensionID;
    }
}
