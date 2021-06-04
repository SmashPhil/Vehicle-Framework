using Verse;
using UnityEngine;

namespace Vehicles.AI
{
	public sealed class VehicleMapping : MapComponent
	{
		public VehicleMapping(Map map) : base(map)
		{
		}

		public VehiclePathGrid VehiclePathGrid { get; private set; }

		public VehiclePathFinder ShipPathFinder { get; private set; }

		public VehiclePathFinder ThreadedPathFinderConstrained { get; private set; }

		public VehicleReachability VehicleReachability { get; private set; }

		public VehicleRegionGrid VehicleRegionGrid { get; private set; }

		public VehicleRegionMaker VehicleRegionMaker { get; private set; }

		public VehicleRegionLinkDatabase VehicleRegionLinkDatabase { get; private set; }

		public VehicleRegionAndRoomUpdater VehicleRegionAndRoomUpdater { get; private set; }

		public VehicleRegionDirtyer VehicleRegionDirtyer { get; private set; }


		public override void FinalizeInit()
		{
			ConstructComponents();
			VehiclePathGrid.RecalculateAllPerceivedPathCosts();
			VehicleRegionAndRoomUpdater.Enabled = true;
			VehicleRegionAndRoomUpdater.RebuildAllWaterRegions();
		}

		public void ConstructComponents()
		{
			VehiclePathGrid = new VehiclePathGrid(map);
			ShipPathFinder = new VehiclePathFinder(map);
			ThreadedPathFinderConstrained = new VehiclePathFinder(map, false);
			VehicleReachability = new VehicleReachability(map);
			VehicleRegionGrid = new VehicleRegionGrid(map);
			VehicleRegionMaker = new VehicleRegionMaker(map);
			VehicleRegionAndRoomUpdater = new VehicleRegionAndRoomUpdater(map);
			VehicleRegionLinkDatabase = new VehicleRegionLinkDatabase();
			VehicleRegionDirtyer = new VehicleRegionDirtyer(map);
		}
	}
}
