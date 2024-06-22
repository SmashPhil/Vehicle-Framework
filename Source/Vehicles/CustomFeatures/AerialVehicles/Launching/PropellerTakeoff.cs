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

		public virtual float TimeInAnimationPropeller
		{
			get
			{
				int ticks = CurAnimationProperties_Propeller.maxTicksPropeller;
				if (ticks <= 0)
				{
					return 0;
				}
				return (float)ticksPassedPropeller / ticks;
			}
		}

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
			float rotationRate = RotationRate(TimeInAnimationPropeller);
			vehicle.graphicOverlay.rotationRegistry.UpdateRegistry(rotationRate);
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
			float rotationRate = RotationRate(TimeInAnimationPropeller);
			vehicle.graphicOverlay.rotationRegistry.UpdateRegistry(rotationRate);
		}

		protected override void TickEvents()
		{
			base.TickEvents();
			if (!CurAnimationProperties_Propeller.eventsPropeller.NullOrEmpty())
			{
				for (int i = 0; i < CurAnimationProperties_Propeller.eventsPropeller.Count; i++)
				{
					AnimationEvent<LaunchProtocol> @event = CurAnimationProperties_Propeller.eventsPropeller[i];
					if (@event.EventFrame(TimeInAnimationPropeller))
					{
						@event.method.Invoke(null, this);
					}
				}
			}
		}

		protected override int AnimationEditorTick_Landing(int ticksPassed)
		{
			ticksPassed = base.AnimationEditorTick_Landing(ticksPassed);
			ticksPassedPropeller = ticksPassed.Take(LandingProperties_Propeller.maxTicksPropeller, out int remaining);
			float rotationRate = RotationRate(TimeInAnimationPropeller);
			vehicle.graphicOverlay.rotationRegistry.UpdateRegistry(rotationRate);
			return remaining;
		}

		protected override int AnimationEditorTick_Takeoff(int ticksPassed)
		{
			ticksPassedPropeller = ticksPassed.Take(LaunchProperties_Propeller.maxTicksPropeller, out int remaining);
			float rotationRate = RotationRate(TimeInAnimationPropeller);
			vehicle.graphicOverlay.rotationRegistry.UpdateRegistry(rotationRate);
			return base.AnimationEditorTick_Takeoff(remaining);
		}

		protected override (Vector3 drawPos, float rotation, DynamicShadowData shadowData) AnimateLanding(Vector3 drawPos, float rotation, DynamicShadowData shadowData)
		{
			if (!LandingProperties_Propeller.rotationPropellerCurve.NullOrEmpty())
			{
				rotation += LandingProperties_Propeller.rotationPropellerCurve.Evaluate(TimeInAnimationPropeller);
			}
			if (!LandingProperties_Propeller.zPositionPropellerCurve.NullOrEmpty())
			{
				drawPos.z += LandingProperties_Propeller.zPositionPropellerCurve.Evaluate(TimeInAnimationPropeller);
			}
			if (!LandingProperties_Propeller.xPositionPropellerCurve.NullOrEmpty())
			{
				drawPos.x += LandingProperties_Propeller.xPositionPropellerCurve.Evaluate(TimeInAnimationPropeller);
			}
			if (!LandingProperties_Propeller.offsetPropellerCurve.NullOrEmpty())
			{
				Vector2 offset = LandingProperties_Propeller.offsetPropellerCurve.EvaluateT(TimeInAnimationPropeller);
				drawPos += new Vector3(offset.x, 0, offset.y);
			}

			if (LandingProperties_Propeller.renderShadow)
			{
				if (!LandingProperties_Propeller.shadowSizeXPropellerCurve.NullOrEmpty())
				{
					shadowData.width = LandingProperties_Propeller.shadowSizeXPropellerCurve.Evaluate(TimeInAnimationPropeller);
				}
				if (!LandingProperties_Propeller.shadowSizeZPropellerCurve.NullOrEmpty())
				{
					shadowData.height = LandingProperties_Propeller.shadowSizeZPropellerCurve.Evaluate(TimeInAnimationPropeller);
				}
				if (!LandingProperties_Propeller.shadowAlphaPropellerCurve.NullOrEmpty())
				{
					shadowData.alpha = LandingProperties_Propeller.shadowAlphaPropellerCurve.Evaluate(TimeInAnimationPropeller);
				}
			}

			return base.AnimateLanding(drawPos, rotation, shadowData);
		}

		protected override (Vector3 drawPos, float rotation, DynamicShadowData shadowData) AnimateTakeoff(Vector3 drawPos, float rotation, DynamicShadowData shadowData)
		{
			if (!LaunchProperties_Propeller.rotationPropellerCurve.NullOrEmpty())
			{
				rotation += LaunchProperties_Propeller.rotationPropellerCurve.Evaluate(TimeInAnimationPropeller);
			}
			if (!LaunchProperties_Propeller.zPositionPropellerCurve.NullOrEmpty())
			{
				drawPos.z += LaunchProperties_Propeller.zPositionPropellerCurve.Evaluate(TimeInAnimationPropeller);
			}
			if (!LaunchProperties_Propeller.xPositionPropellerCurve.NullOrEmpty())
			{
				drawPos.x += LaunchProperties_Propeller.xPositionPropellerCurve.Evaluate(TimeInAnimationPropeller);
			}
			if (!LaunchProperties_Propeller.offsetPropellerCurve.NullOrEmpty())
			{
				Vector2 offset = LaunchProperties_Propeller.offsetPropellerCurve.EvaluateT(TimeInAnimationPropeller);
				drawPos += new Vector3(offset.x, 0, offset.y);
			}

			if (LaunchProperties_Propeller.renderShadow)
			{
				if (!LaunchProperties_Propeller.shadowSizeXPropellerCurve.NullOrEmpty())
				{
					shadowData.width = LaunchProperties_Propeller.shadowSizeXPropellerCurve.Evaluate(TimeInAnimationPropeller);
				}
				if (!LaunchProperties_Propeller.shadowSizeZPropellerCurve.NullOrEmpty())
				{
					shadowData.height = LaunchProperties_Propeller.shadowSizeZPropellerCurve.Evaluate(TimeInAnimationPropeller);
				}
				if (!LaunchProperties_Propeller.shadowAlphaPropellerCurve.NullOrEmpty())
				{
					shadowData.alpha = LaunchProperties_Propeller.shadowAlphaPropellerCurve.Evaluate(TimeInAnimationPropeller);
				}
			}

			return base.AnimateTakeoff(drawPos, rotation, shadowData);
		}

		public override bool FinishedAnimation(VehicleSkyfaller skyfaller)
		{
			return ticksPassedPropeller >= CurAnimationProperties_Propeller.maxTicksPropeller && base.FinishedAnimation(skyfaller);
		}

		protected override void PreAnimationSetup()
		{
			base.PreAnimationSetup();
			ticksPassedPropeller = 0;
			effectsToThrowPropeller = 0;
			if (launchType == LaunchType.Landing)
			{
				vehicle.graphicOverlay.rotationRegistry.Reset();
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref ticksPassedPropeller, nameof(ticksPassedPropeller), 0);
			Scribe_Values.Look(ref effectsToThrowPropeller, nameof(effectsToThrowPropeller), 0);
		}
	}
}
