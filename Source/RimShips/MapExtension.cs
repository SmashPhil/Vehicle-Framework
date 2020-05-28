using Verse;


namespace Vehicles.AI
{
    public sealed class MapExtension : IExposable
    {
        public MapExtension(Map map) //: base(map)
        {
            this.map = map;
        }

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

        public void VerifyComponents()
        {
            if (this.shipPathGrid is null) this.shipPathGrid = new ShipPathGrid(this.map);
            if (this.shipPathFinder is null) this.shipPathFinder = new ShipPathFinder(this.map);
            if (this.shipReachability is null) this.shipReachability = new ShipReachability(this.map);
            if (this.waterRegionGrid is null) this.waterRegionGrid = new WaterRegionGrid(this.map);
            if (this.waterRegionMaker is null) this.waterRegionMaker = new WaterRegionMaker(this.map);
            if (this.waterRegionAndRoomUpdater is null) this.waterRegionAndRoomUpdater = new WaterRegionAndRoomUpdater(this.map);
            if (this.waterRegionLinkDatabase is null) this.waterRegionLinkDatabase = new WaterRegionLinkDatabase();
        }

        public void ExposeData()
        {
            Scribe_Deep.Look<ShipPathGrid>(ref this.shipPathGrid, "shipPathGrid", this.map, false);
            Scribe_Deep.Look<ShipPathFinder>(ref this.shipPathFinder, "shipPathFinder", this.map, this, false);
            Scribe_Deep.Look<ShipReachability>(ref this.shipReachability, "shipReachability", this.map, this, false);
            Scribe_Deep.Look<WaterRegionMaker>(ref this.waterRegionMaker, "waterRegionMaker", this.map, this, false);
            Scribe_Deep.Look<WaterRegionGrid>(ref this.waterRegionGrid, "waterRegionGrid", this.map, this, false);
            Scribe_Deep.Look<WaterRegionLinkDatabase>(ref this.waterRegionLinkDatabase, "waterRegionLinkDatabase", false);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.ConstructComponents();
            }
        }

        private ShipPathGrid shipPathGrid;

        private ShipPathFinder shipPathFinder;

        private ShipReachability shipReachability;

        private WaterRegionMaker waterRegionMaker;

        private WaterRegionGrid waterRegionGrid;

        private WaterRegionLinkDatabase waterRegionLinkDatabase;

        private WaterRegionAndRoomUpdater waterRegionAndRoomUpdater;

        private readonly Map map;
    }
}
