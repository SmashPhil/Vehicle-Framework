using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;
using System.Runtime;

namespace Vehicles
{
	public class TurretUpgrade : Upgrade
	{
		public readonly List<VehicleTurret> turrets;

		public readonly List<string> removeTurrets;
		
		public override bool UnlockOnLoad => false;

		public override void Unlock(VehiclePawn vehicle, bool unlockingPostLoad)
		{
			if (!unlockingPostLoad)
			{
				if (!removeTurrets.NullOrEmpty())
				{
					foreach (string key in removeTurrets)
					{
						if (!vehicle.CompVehicleTurrets.RemoveTurret(key))
						{
							Log.Error($"Unable to remove {key} from {vehicle}. Turret not found.");
						}
					}
				}
				if (!turrets.NullOrEmpty())
				{
					foreach (VehicleTurret turret in turrets)
					{
						try
						{
							vehicle.CompVehicleTurrets.AddTurret(turret, node.key);
						}
						catch (Exception ex)
						{
							Log.Error($"{VehicleHarmony.LogLabel} Unable to unlock {GetType()} to {vehicle.LabelShort}. \nException: {ex}");
						}
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
						Log.Error($"Unable to remove {turret.key} from {vehicle}. Turret not found.");
					}
				}
			}
			if (!removeTurrets.NullOrEmpty())
			{
				foreach (string key in removeTurrets)
				{
					VehicleTurret turret = vehicle.CompVehicleTurrets.Props.turrets.FirstOrDefault(turret => turret.key == key);
					if (turret == null)
					{
						Log.Error($"Unable to add {key} to {vehicle}. Turret must be defined in the VehicleDef in order to be re-added post-refund.");
					}
					else
					{
						vehicle.CompVehicleTurrets.AddTurret(turret);
					}
				}
			}
			vehicle.CompVehicleTurrets.CheckDuplicateKeys();
		}
	}
}
