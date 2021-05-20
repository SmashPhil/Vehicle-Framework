using Verse;
using UnityEngine;

namespace Vehicles.AI
{
	public sealed class WaterMap : MapComponent
	{
		public WaterMap(Map map) : base(map)
		{
		}

		public ShipPathGrid ShipPathGrid { get; private set; }

		public VehiclePathFinder ShipPathFinder { get; private set; }

		public VehiclePathFinder ThreadedPathFinderConstrained { get; private set; }

		public ShipReachability ShipReachability { get; private set; }

		public WaterRegionGrid WaterRegionGrid { get; private set; }

		public WaterRegionMaker WaterRegionmaker { get; private set; }

		public WaterRegionLinkDatabase WaterRegionLinkDatabase { get; private set; }

		public WaterRegionAndRoomUpdater WaterRegionAndRoomUpdater { get; private set; }

		public WaterRegionDirtyer WaterRegionDirtyer { get; private set; }


		public override void FinalizeInit()
		{
			ConstructComponents();
			ShipPathGrid.RecalculateAllPerceivedPathCosts();
			WaterRegionAndRoomUpdater.Enabled = true;
			WaterRegionAndRoomUpdater.RebuildAllWaterRegions();
		}

		public void ConstructComponents()
		{
			ShipPathGrid = new ShipPathGrid(map);
			ShipPathFinder = new VehiclePathFinder(map);
			ThreadedPathFinderConstrained = new VehiclePathFinder(map, false);
			ShipReachability = new ShipReachability(map);
			WaterRegionGrid = new WaterRegionGrid(map);
			WaterRegionmaker = new WaterRegionMaker(map);
			WaterRegionAndRoomUpdater = new WaterRegionAndRoomUpdater(map);
			WaterRegionLinkDatabase = new WaterRegionLinkDatabase();
			WaterRegionDirtyer = new WaterRegionDirtyer(map);
		}
	}
}
