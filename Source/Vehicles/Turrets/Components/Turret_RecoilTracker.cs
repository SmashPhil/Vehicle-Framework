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
		private float curRecoil;

		private float targetRecoil;
		private float recoilStep;
		private bool recoilingBack;

		private readonly RecoilProperties recoilProperties;

		public Turret_RecoilTracker(RecoilProperties recoilProperties)
		{
			this.recoilProperties = recoilProperties;
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
					curRecoil -= recoilStep * recoilProperties.speedMultiplierPostRecoil;
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

		public void Notify_TurretRecoil(float angle)
		{
			targetRecoil = recoilProperties.distanceTotal;
			recoilStep = recoilProperties.distancePerTick;
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
