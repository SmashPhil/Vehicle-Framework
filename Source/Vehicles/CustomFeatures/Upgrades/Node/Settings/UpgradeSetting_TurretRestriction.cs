using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Vehicles
{
	public class UpgradeSetting_TurretRestriction : UpgradeSetting_Turret
	{
		public Type restrictionType;
		public Operation operation;

		public override void Unlocked(VehiclePawn vehicle, bool unlockingPostLoad)
		{
			VehicleTurret turret = vehicle.CompVehicleTurrets.GetTurret(turretKey);
			if (turret == null)
			{
				Log.ErrorOnce($"Unable to locate turret with key={turretKey}. The turret must be part of the vehicle to add upgrades.", turretKey.GetHashCodeSafe());
				return;
			}
			if (restrictionType != null)
			{
				switch (operation)
				{
					case Operation.Add:
						turret.SetTurretRestriction(restrictionType);
						break;
					case Operation.Remove:
						turret.RemoveTurretRestriction();
						break;
				}
			}
		}

		public override void Refunded(VehiclePawn vehicle)
		{
			VehicleTurret turret = vehicle.CompVehicleTurrets.GetTurret(turretKey);
			if (turret == null)
			{
				Log.ErrorOnce($"Unable to locate turret with key={turretKey}. The turret must be part of the vehicle to add upgrades.", turretKey.GetHashCodeSafe());
				return;
			}
			if (restrictionType != null)
			{
				switch (operation)
				{
					case Operation.Add:
						turret.RemoveTurretRestriction();
						break;
					case Operation.Remove:
						turret.SetTurretRestriction(restrictionType);
						break;
				}
			}
		}

		public enum Operation
		{
			Add,
			Remove,
		}
	}
}
