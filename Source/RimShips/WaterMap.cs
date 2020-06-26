using Verse;
using UnityEngine;

namespace Vehicles.AI
{
    public sealed class WaterMap : MapComponent
    {
        public WaterMap(Map map) : base(map)
        {
            ConstructComponents();
        }

        public ShipPathGrid getShipPathGrid { get; private set; }

        public VehiclePathFinder getShipPathFinder { get; private set; }

        public VehiclePathFinder threadedPathFinderConstrained { get; private set; }

        public ShipReachability getShipReachability { get; private set; }

        public WaterRegionGrid getWaterRegionGrid { get; private set; }

        public WaterRegionMaker getWaterRegionmaker { get; private set; }

        public WaterRegionLinkDatabase getWaterRegionLinkDatabase { get; private set; }

        public WaterRegionAndRoomUpdater getWaterRegionAndRoomUpdater { get; private set; }

        public override void FinalizeInit()
        {
            getShipPathGrid.RecalculateAllPerceivedPathCosts();
            getWaterRegionAndRoomUpdater.Enabled = true;
            getWaterRegionAndRoomUpdater.RebuildAllWaterRegions();
        }

        public void ConstructComponents()
        {
            getShipPathGrid = new ShipPathGrid(map);
            getShipPathFinder = new VehiclePathFinder(map);
            threadedPathFinderConstrained = new VehiclePathFinder(map, false);
            getShipReachability = new ShipReachability(map);
            getWaterRegionGrid = new WaterRegionGrid(map);
            getWaterRegionmaker = new WaterRegionMaker(map);
            getWaterRegionAndRoomUpdater = new WaterRegionAndRoomUpdater(map);
            getWaterRegionLinkDatabase = new WaterRegionLinkDatabase();
        }
    }
}
