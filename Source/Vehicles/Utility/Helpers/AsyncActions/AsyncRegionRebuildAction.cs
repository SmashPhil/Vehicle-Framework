using SmashTools.Performance;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static Vehicles.VehicleMapping;

namespace Vehicles
{
	public class AsyncRegionRebuildAction : AsyncAction
	{
		private VehicleMapping mapping;
		private VehiclePathData pathData;

		public override bool IsValid => mapping?.map?.Index > -1;

		public void Set(VehicleMapping mapping, VehiclePathData pathData)
		{
			this.mapping = mapping;
			this.pathData = pathData;
		}

		public override void Invoke()
		{
			pathData.VehicleRegionGrid.UpdateClean();
			pathData.VehicleRegionAndRoomUpdater.TryRebuildVehicleRegions();
		}

		public override void ReturnToPool()
		{
			mapping = null;
			pathData = null;
			AsyncPool<AsyncRegionRebuildAction>.Return(this);
		}
	}
}
