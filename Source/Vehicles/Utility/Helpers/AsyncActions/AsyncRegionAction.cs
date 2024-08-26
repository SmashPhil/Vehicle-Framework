using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;
using SmashTools.Performance;

namespace Vehicles
{
	public class AsyncRegionAction : AsyncAction
	{
		private VehicleMapping mapping;
		private List<VehicleDef> vehicleDefs;
		private CellRect cellRect;
		private bool spawned;

		public override bool IsValid => mapping?.map?.Index > -1;

		public void Set(VehicleMapping mapping, List<VehicleDef> vehicleDefs, CellRect cellRect, bool spawned)
		{
			this.mapping = mapping;
			this.vehicleDefs = vehicleDefs;
			this.cellRect = cellRect;
			this.spawned = spawned;
		}

		public override void Invoke()
		{
			if (spawned)
			{
				PathingHelper.ThingInRegionSpawned(cellRect, mapping, vehicleDefs);
				
			}
			else
			{
				PathingHelper.ThingInRegionDespawned(cellRect, mapping, vehicleDefs);
			}
		}

		public override void ReturnToPool()
		{
			mapping = null;
			vehicleDefs = null;
			AsyncPool<AsyncRegionAction>.Return(this);
		}
	}
}
