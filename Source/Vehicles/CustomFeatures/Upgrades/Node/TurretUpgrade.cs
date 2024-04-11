using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class TurretUpgrade : Upgrade
	{
		public readonly List<VehicleTurret> turrets;

		public readonly List<string> removeTurrets;

		public override bool UnlockOnLoad => false;

		public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
		{
			if (!turrets.NullOrEmpty())
			{
				foreach (VehicleTurret turret in turrets)
				{
					try
					{
						vehicle.CompVehicleTurrets.AddTurret(turret);
					}
					catch (Exception ex)
					{
						Log.Error($"{VehicleHarmony.LogLabel} Unable to unlock {GetType()} to {vehicle.LabelShort}. \nException: {ex}");
					}
				}
			}
			if (!removeTurrets.NullOrEmpty())
			{
				foreach (string key in removeTurrets)
				{
					if (!vehicle.CompVehicleTurrets.RemoveTurret(key))
					{
						Log.Warning($"Unable to remove {key} from {vehicle}. Turret not found.");
					}
				}
			}
			vehicle.CompVehicleTurrets.CheckDuplicateKeys();
		}

		public override void Refund(VehiclePawn vehicle)
		{
			if (!turrets.NullOrEmpty())
			{
				foreach (VehicleTurret turret in turrets)
				{
					if (!vehicle.CompVehicleTurrets.RemoveTurret(turret.key))
					{
						Log.Warning($"Unable to remove {turret.key} from {vehicle}. Turret not found.");
					}
				}
			}
			vehicle.CompVehicleTurrets.CheckDuplicateKeys();
		}
	}
}
