using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class PropellerTakeoff : VTOLTakeoff
	{
		public float accelerationRate = 1;
		public FloatRange rotationalAcceleration;

		protected int rotatingTicksPassed = 0;

		public PropellerTakeoff()
		{
		}

		public PropellerTakeoff(PropellerTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
		{
			accelerationRate = reference.accelerationRate;
			rotationalAcceleration = reference.rotationalAcceleration;
		}

		protected virtual float RateOfDecay => 100;

		protected virtual float RotationRateForTick => Mathf.Exp(accelerationRate * (TicksPassed + TicksVertical + rotatingTicksPassed) / RateOfDecay).Clamp(Graphic_Rotator.MinRotationStep, Graphic_Rotator.MaxRotationStep);

		public virtual float TicksVertical => ticksPassedVertical;

		protected override void TickTakeoff()
		{
			if (RotationRateForTick >= Graphic_Rotator.MaxRotationStep)
			{
				base.TickTakeoff();
			}
			else
			{
				rotatingTicksPassed++;
			}
			vehicle.graphicOverlay.rotationRegistry[Graphic_Propeller.Key] += RotationRateForTick;
		}

		protected override void TickLanding()
		{
			if (RotationRateForTick >= Graphic_Rotator.MaxRotationStep)
			{
				base.TickLanding();
			}
			else
			{
				rotatingTicksPassed--;
			}
			vehicle.graphicOverlay.rotationRegistry[Graphic_Propeller.Key] += RotationRateForTick;
		}

		public override bool FinishedLanding(VehicleSkyfaller skyfaller)
		{
			return rotatingTicksPassed <= 0 && base.FinishedLanding(skyfaller);
		}

		protected override void PreAnimationSetup()
		{
			base.PreAnimationSetup();
			rotatingTicksPassed = landing ? Mathf.CeilToInt(RateOfDecay * Mathf.Log(Graphic_Rotator.MaxRotationStep) / accelerationRate) : 0;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref accelerationRate, "accelerationRate");
			Scribe_Values.Look(ref rotationalAcceleration, "rotationalAcceleration");
			Scribe_Values.Look(ref rotatingTicksPassed, "rotatingTicksPassed", 0);
		}
	}
}
