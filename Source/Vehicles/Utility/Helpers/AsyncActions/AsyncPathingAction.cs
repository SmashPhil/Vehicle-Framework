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
		private IntVec3 position;

		public override bool IsValid => mapping?.map?.Index > -1;

		public void Set(VehicleMapping mapping, IntVec3 position)
		{
			this.mapping = mapping;
			this.position = position;
		}

		public override void Invoke()
		{
			PathingHelper.RecalculatePerceivedPathCostAtFor(mapping, position);
		}

		public override void ReturnToPool()
		{
			mapping = null;
			AsyncPool<AsyncPathingAction>.Return(this);
		}
	}
}
