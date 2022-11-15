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
		public const float DefaultAccelerationRate = 0.65f;
		public const float MinRotationStep = 0.1f;
		public const float MaxRotationStep = 59;

		protected int ticksPassedPropeller = 0;
		protected float effectsToThrowPropeller = 0;

		public PropellerTakeoff()
		{
		}

		public PropellerTakeoff(PropellerTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
		{
		}

		public PropellerProtocolProperties LandingProperties_Propeller => landingProperties as PropellerProtocolProperties;
		public PropellerProtocolProperties LaunchProperties_Propeller => launchProperties as PropellerProtocolProperties;
		public PropellerProtocolProperties CurAnimationProperties_Propeller => CurAnimationProperties as PropellerProtocolProperties;

		protected override int TotalTicks_Landing => base.TotalTicks_Landing + LandingProperties_Propeller.maxTicksPropeller;
		protected override int TotalTicks_Takeoff => base.TotalTicks_Takeoff + LaunchProperties_Propeller.maxTicksPropeller;

		public virtual float TicksVertical => ticksPassedVertical;

		public virtual float TimeInAnimationPropeller => (float)ticksPassedPropeller / CurAnimationProperties_Propeller.maxTicksPropeller;

		protected virtual float RotationRate(float t)
		{
			return CurAnimationProperties_Propeller.angularVelocityPropeller.Evaluate(t);
		}

		protected override void TickMotes()
		{
			FleckData fleckData = CurAnimationProperties_Propeller.fleckDataPropeller;
			if (fleckData != null && (fleckData.runOutOfStep || (TimeInAnimationPropeller > 0 && TimeInAnimationPropeller < 1)))
			{
				effectsToThrowPropeller = TryThrowFleck(fleckData, TimeInAnimationPropeller, effectsToThrowPropeller);
			}
			base.TickMotes();
		}

		protected override void TickLanding()
		{
			if (ticksPassedVertical < LandingProperties_Propeller.maxTicksVertical)
			{
				base.TickLanding();
			}
			else
			{
				ticksPassedPropeller++;
				TickMotes();
			}
			vehicle.graphicOverlay.rotationRegistry[Graphic_Propeller.Key] += RotationRate(TimeInAnimationPropeller);
		}

		protected override void TickTakeoff()
		{
			if (ticksPassedPropeller >= LaunchProperties_Propeller.maxTicksPropeller)
			{
				base.TickTakeoff();
			}
			else
			{
				ticksPassedPropeller++;
				TickMotes();
			}
			vehicle.graphicOverlay.rotationRegistry[Graphic_Propeller.Key] += RotationRate(TimeInAnimationPropeller);
		}

		protected override int AnimationEditorTick_Landing(int ticksPassed)
		{
			ticksPassed = base.AnimationEditorTick_Landing(ticksPassed);
			ticksPassedPropeller = ticksPassed.Take(LandingProperties_Propeller.maxTicksPropeller, out int remaining);
			vehicle.graphicOverlay.rotationRegistry[Graphic_Propeller.Key] += RotationRate(TimeInAnimationPropeller);
			return remaining;
		}

		protected override int AnimationEditorTick_Takeoff(int ticksPassed)
		{
			ticksPassedPropeller = ticksPassed.Take(LaunchProperties_Propeller.maxTicksPropeller, out int remaining);
			vehicle.graphicOverlay.rotationRegistry[Graphic_Propeller.Key] += RotationRate(TimeInAnimationPropeller);
			return base.AnimationEditorTick_Takeoff(remaining);
		}

		public override bool FinishedAnimation(VehicleSkyfaller skyfaller)
		{
			return ticksPassedPropeller >= CurAnimationProperties.maxTicks && base.FinishedAnimation(skyfaller);
		}

		protected override void PreAnimationSetup()
		{
			base.PreAnimationSetup();
			ticksPassedPropeller = 0;
			effectsToThrowPropeller = 0;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref ticksPassedPropeller, nameof(ticksPassedPropeller), 0);
			Scribe_Values.Look(ref effectsToThrowPropeller, nameof(effectsToThrowPropeller), 0);
		}
	}
}
