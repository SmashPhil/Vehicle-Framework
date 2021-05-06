using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	public class VehicleCaravan_Tweener
	{
		private const float SpringTightness = 0.09f;

		private readonly VehicleCaravan caravan;
		private Vector3 tweenedPos = Vector3.zero;
		private Vector3 lastTickSpringPos;

		public VehicleCaravan_Tweener(VehicleCaravan caravan)
		{
			this.caravan = caravan;
		}

		public Vector3 TweenedPos
		{
			get
			{
				return tweenedPos;
			}
		}

		public Vector3 LastTickTweenedVelocity
		{
			get
			{
				return TweenedPos - lastTickSpringPos;
			}
		}

		public Vector3 TweenedPosRoot
		{
			get
			{
				return VehicleCaravanTweenerUtility.PatherTweenedPosRoot(caravan) + VehicleCaravanTweenerUtility.CaravanCollisionPosOffsetFor(caravan);
			}
		}

		public void TweenerTick()
		{
			lastTickSpringPos = tweenedPos;
			Vector3 a = TweenedPosRoot - tweenedPos;
			tweenedPos += a * SpringTightness;
		}

		public void ResetTweenedPosToRoot()
		{
			tweenedPos = TweenedPosRoot;
			lastTickSpringPos = tweenedPos;
		}
	}
}
