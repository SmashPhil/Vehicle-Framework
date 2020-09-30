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
		public float Recoil => curRecoil;
		public float Angle => recoilAngle;

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

        public void Notify_CannonRecoil(CannonHandler cannonHandler, float angle)
        {
			targetRecoil = cannonHandler.cannonDef.recoil;
			recoilStep = targetRecoil / 5;
			curRecoil = 0;
			recoilingBack = true;
			recoilAngle = angle;
        }
		
		private void ResetRecoilVars()
        {
			curRecoil = 0;
			targetRecoil = 0;
			recoilStep = 0;
			recoilingBack = true;
			recoilAngle = 0;
        }

		private const float RecoilDeteriorationRate = 0.5f;

		public float curRecoil;
		private float targetRecoil;
		private float recoilStep;
		private float recoilAngle;
		private bool recoilingBack;
    }
}
