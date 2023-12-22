using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;
using SmashTools.Performance;

namespace Vehicles
{
	public class AsyncPathFindAction : AsyncAction
	{
		private VehiclePawn vehicle;

		public override bool IsValid => vehicle != null && vehicle.Spawned && vehicle.vehiclePather.Moving && vehicle.vehiclePather.CalculatingPath;

		public void Set(VehiclePawn vehicle)
		{
			this.vehicle = vehicle;
		}

		public override void Invoke()
		{
			vehicle.vehiclePather.TrySetNewPath_Delayed();
		}

		public override void ReturnToPool()
		{
			vehicle = null;
			AsyncPool<AsyncPathFindAction>.Return(this);
		}

		public override void ExceptionThrown(Exception ex)
		{
			vehicle.vehiclePather.CalculatingPath = false;
		}
	}
}
