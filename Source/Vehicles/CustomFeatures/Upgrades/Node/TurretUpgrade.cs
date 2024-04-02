using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public class TurretUpgrade : Upgrade
	{
		private List<VehicleTurret> turrets;

		public override bool UnlockOnLoad => false;

		public override void Unlock(VehiclePawn vehicle)
		{
			//vehicle.CompVehicleTurrets.AddTurrets(turretsUnlocked.Keys.ToList());
			//vehicle.AddHandlers(turretsUnlocked.Values.ToList());
		}

		public override void Refund(VehiclePawn vehicle)
		{
			//vehicle.CompVehicleTurrets.RemoveTurrets(turretsUnlocked.Keys.ToList());
			//vehicle.RemoveHandlers(turretsUnlocked.Values.ToList());
		}
	}
}
