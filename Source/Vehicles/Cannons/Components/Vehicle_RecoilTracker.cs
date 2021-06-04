using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class Vehicle_RecoilTracker
	{
		public float curRecoil;
		private float targetRecoil;
		private float recoilStep;
		private bool recoilingBack;
		private float speedMultiplierPostRecoil;

		public float Angle { get; private set; }

		public float Recoil => curRecoil;

		public void RecoilTick()
		{
			if (targetRecoil > 0f)
			{
				if (recoilingBack)
				{
					curRecoil += recoilStep;
					if (curRecoil >= targetRecoil)
					{
						curRecoil = targetRecoil;
					}
				}
				else
				{
					curRecoil -= recoilStep * speedMultiplierPostRecoil;
					if (curRecoil <= 0)
					{
						ResetRecoilVars();
					}
				}

				if (curRecoil >= targetRecoil)
				{
					recoilingBack = false;
				}
			}
		}

		public void Notify_TurretRecoil(VehicleTurret turret, float angle)
		{
			if (turret.turretDef.vehicleRecoil is null)
			{
				return;
			}
			targetRecoil = turret.turretDef.vehicleRecoil.distanceTotal;
			recoilStep = turret.turretDef.vehicleRecoil.distancePerTick;
			speedMultiplierPostRecoil = turret.turretDef.vehicleRecoil.speedMultiplierPostRecoil;
			curRecoil = 0;
			recoilingBack = true;
			Angle = angle;
		}
		
		private void ResetRecoilVars()
		{
			targetRecoil = 0;
			recoilStep = 0;
			speedMultiplierPostRecoil = 0;
			curRecoil = 0;
			Angle = 0;
		}
	}
}
