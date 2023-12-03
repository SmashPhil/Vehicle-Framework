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
		private List<List<Thing>> snapshotLists;
		private CellRect cellRect;
		private bool spawned;

		public override bool IsValid => mapping?.map?.Index > -1;

		public void Set(VehicleMapping mapping, List<VehicleDef> vehicleDefs, List<List<Thing>> snapshotLists, CellRect cellRect, bool spawned)
		{
			this.mapping = mapping;
			this.vehicleDefs = vehicleDefs;
			this.snapshotLists = snapshotLists;
			this.cellRect = cellRect;
			this.spawned = spawned;
		}

		public override void Invoke()
		{
			if (spawned)
			{
				PathingHelper.ThingInRegionSpawned(cellRect, mapping, vehicleDefs, snapshotLists);
				
			}
			else
			{
				PathingHelper.ThingInRegionDespawned(cellRect, mapping, vehicleDefs, snapshotLists);
			}

			for (int i = snapshotLists.Count - 1; i >= 0; i--)
			{
				List<Thing> thingList = snapshotLists[i];
				thingList.Clear();
				AsyncPool<List<Thing>>.Return(thingList);
			}
			snapshotLists.Clear();
			AsyncPool<List<List<Thing>>>.Return(snapshotLists);
		}

		public override void ReturnToPool()
		{
			mapping = null;
			vehicleDefs = null;
			snapshotLists = null;
			AsyncPool<AsyncRegionAction>.Return(this);
		}
	}
}
