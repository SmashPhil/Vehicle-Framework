using Verse;
using UnityEngine;

namespace Vehicles.AI
{
    public sealed class MapExtension : MapComponent
    {
        public MapExtension(Map map) : base(map)
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

        public void VerifyComponents()
        {
            if (getShipPathGrid is null) getShipPathGrid = new ShipPathGrid(map);
            if (getShipPathFinder is null) getShipPathFinder = new VehiclePathFinder(map);
            if (threadedPathFinderConstrained is null) threadedPathFinderConstrained = new VehiclePathFinder(map, false);
            if (getShipReachability is null) getShipReachability = new ShipReachability(map);
            if (getWaterRegionGrid is null) getWaterRegionGrid = new WaterRegionGrid(map);
            if (getWaterRegionmaker is null) getWaterRegionmaker = new WaterRegionMaker(map);
            if (getWaterRegionAndRoomUpdater is null) getWaterRegionAndRoomUpdater = new WaterRegionAndRoomUpdater(map);
            if (getWaterRegionLinkDatabase is null) getWaterRegionLinkDatabase = new WaterRegionLinkDatabase();
        }
    }
}
