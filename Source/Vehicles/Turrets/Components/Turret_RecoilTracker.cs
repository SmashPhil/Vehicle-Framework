using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
	public class Turret_RecoilTracker
	{
		public float curRecoil;
		protected float targetRecoil;
		protected float recoilStep;
		protected bool recoilingBack;

		protected VehicleTurret turret;

		public Turret_RecoilTracker(VehicleTurret turret)
		{
			this.turret = turret;
		}

		public float Angle { get; private set; }

		public float Recoil => curRecoil;

		public bool RecoilTick()
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
					curRecoil -= recoilStep * turret.turretDef.recoil.speedMultiplierPostRecoil;
					if (curRecoil <= 0)
					{
						ResetRecoilVars();
						return false;
					}
				}

				if (curRecoil >= targetRecoil)
				{
					recoilingBack = false;
				}
				return true;
			}
			return false;
		}

		public void Notify_TurretRecoil(VehicleTurret turret, float angle)
		{
			if (!turret.Recoils)
			{
				return;
			}
			targetRecoil = turret.turretDef.recoil.distanceTotal;
			recoilStep = turret.turretDef.recoil.distancePerTick;
			curRecoil = 0;
			recoilingBack = true;
			Angle = angle;
		}
		
		private void ResetRecoilVars()
		{
			curRecoil = 0;
			targetRecoil = 0;
			recoilStep = 0;
			Angle = 0;
		}
	}
}
