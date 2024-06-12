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

		public readonly List<TurretSettings> settings;

		public override bool UnlockOnLoad => false;

		public override void Unlock(VehiclePawn vehicle, bool unlockingAfterLoad)
		{
			if (!unlockingAfterLoad)
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
							vehicle.CompVehicleTurrets.AddTurret(turret);
						}
						catch (Exception ex)
						{
							Log.Error($"{VehicleHarmony.LogLabel} Unable to unlock {GetType()} to {vehicle.LabelShort}. \nException: {ex}");
						}
					}
				}
			}
			if (!settings.NullOrEmpty())
			{
				foreach (TurretSettings setting in settings)
				{
					VehicleTurret turret = vehicle.CompVehicleTurrets.GetTurret(setting.key);
					if (turret == null)
					{
						Log.ErrorOnce($"Unable to locate turret with key={setting.key}. The turret must be part of the vehicle to add upgrades.", setting.key.GetHashCodeSafe());
						continue;
					}
					if (setting.restrictions != null)
					{
						switch (setting.restrictions.Value.operation)
						{
							case TurretRestrictionOperation.Add:
								turret.SetTurretRestriction(setting.restrictions.Value.restrictionType);
								break;
							case TurretRestrictionOperation.Remove:
								turret.RemoveTurretRestriction();
								break;
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
			if (!settings.NullOrEmpty())
			{
				foreach (TurretSettings setting in settings)
				{
					VehicleTurret turret = vehicle.CompVehicleTurrets.GetTurret(setting.key);
					if (turret == null)
					{
						Log.ErrorOnce($"Unable to locate turret with key={setting.key}. The turret must be part of the vehicle to add upgrades.", setting.key.GetHashCodeSafe());
						continue;
					}
					if (setting.restrictions != null)
					{
						switch (setting.restrictions.Value.operation)
						{
							case TurretRestrictionOperation.Add:
								turret.RemoveTurretRestriction();
								break;
							case TurretRestrictionOperation.Remove:
								turret.SetTurretRestriction(setting.restrictions.Value.restrictionType);
								break;
						}
					}
				}
			}
			vehicle.CompVehicleTurrets.CheckDuplicateKeys();
		}

		public enum TurretRestrictionOperation 
		{ 
			Add,
			Remove,
		}

		public struct TurretSettings
		{
			public string key;

			public TurretRestrictionSetting? restrictions;
		}

		public struct TurretRestrictionSetting
		{
			public Type restrictionType;
			public TurretRestrictionOperation operation;
		}
	}
}
