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
		private const float RecoilDeteriorationRate = 0.5f;

		public float curRecoil;
		private float targetRecoil;
		private float recoilStep;
		private bool recoilingBack;

		public float Angle { get; private set; }

		public float Recoil => curRecoil;

		public void RecoilTick()
		{
			if (targetRecoil > 0f)
			{
				if (recoilingBack)
				{
					curRecoil += recoilStep;
				}
				else
				{
					curRecoil -= recoilStep * RecoilDeteriorationRate;
				}

				if (Mathf.Abs(curRecoil - targetRecoil) < recoilStep)
				{
					ResetRecoilVars();
				}
			}
		}

		public void Notify_TurretRecoil(VehicleTurret cannonHandler, float angle)
		{
			targetRecoil = cannonHandler.turretDef.vehicleRecoil;
			recoilStep = targetRecoil / 5;
			curRecoil = 0;
			recoilingBack = true;
			Angle = angle;
		}
		
		private void ResetRecoilVars()
		{
			curRecoil = 0;
			targetRecoil = 0;
			recoilStep = 0;
			recoilingBack = false;
			Angle = 0;
		}
	}
}
