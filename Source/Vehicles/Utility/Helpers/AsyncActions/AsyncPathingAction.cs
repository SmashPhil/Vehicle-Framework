using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;
using SmashTools.Performance;

namespace Vehicles
{
	public class AsyncPathingAction : AsyncAction
	{
		private VehicleMapping mapping;
		private List<Thing> snapshotList;
		private IntVec3 position;

		public override bool IsValid => mapping?.map?.Index > -1;

		public void Set(VehicleMapping mapping, List<Thing> snapshotList, IntVec3 position)
		{
			this.mapping = mapping;
			this.snapshotList = snapshotList;
			this.position = position;
		}

		public override void Invoke()
		{
			PathingHelper.RecalculatePerceivedPathCostAtFor(mapping, position, snapshotList);
			snapshotList.Clear();
			AsyncPool<List<Thing>>.Return(snapshotList);
		}

		public override void ReturnToPool()
		{
			mapping = null;
			snapshotList = null;
			AsyncPool<AsyncPathingAction>.Return(this);
		}
	}
}
