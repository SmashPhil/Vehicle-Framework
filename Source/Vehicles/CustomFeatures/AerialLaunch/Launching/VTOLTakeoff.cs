using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class VTOLTakeoff : DefaultTakeoff
	{
		protected int ticksPassedVertical;
		protected float effectsToThrowVertical;

		public VTOLTakeoff()
		{
		}

		public VTOLTakeoff(VTOLTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
		{
		}

		public VerticalProtocolProperties LandingProperties_VTOL => landingProperties as VerticalProtocolProperties;
		public VerticalProtocolProperties LaunchProperties_VTOL => launchProperties as VerticalProtocolProperties;

		public VerticalProtocolProperties CurAnimationProperties_Vertical => CurAnimationProperties as VerticalProtocolProperties;

		public override string FailLaunchMessage => "SkyfallerLaunchNotValid".Translate();

		protected override int TotalTicks_Landing => base.TotalTicks_Landing + LandingProperties_VTOL.maxTicksVertical;

		protected override int TotalTicks_Takeoff => base.TotalTicks_Takeoff + LaunchProperties_VTOL.maxTicksVertical;

		public virtual float TimeInAnimationVTOL => (float)ticksPassedVertical / CurAnimationProperties_Vertical.maxTicksVertical;

		public override Command_Action LaunchCommand
		{
			get
			{
				Command_Action skyfallerTakeoff = new Command_Action
				{
					defaultLabel = "CommandLaunchGroup".Translate(),
					defaultDesc = "CommandLaunchGroupDesc".Translate(),
					icon = VehicleTex.LaunchCommandTex,
					alsoClickIfOtherInGroupClicked = false,
					action = delegate ()
					{
						if (vehicle.CompVehicleLauncher.AnyLeftToLoad)
						{
							Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("ConfirmSendNotCompletelyLoadedPods".Translate(vehicle.LabelCapNoCount), new Action(StartChoosingDestination), false, null));
							return;
						}
						StartChoosingDestination();
					}
				};
				return skyfallerTakeoff;
			}
		}

		public override bool FinishedAnimation(VehicleSkyfaller skyfaller)
		{
			return ticksPassedVertical >= CurAnimationProperties_Vertical.maxTicksVertical && base.FinishedAnimation(skyfaller);
		}

		protected override int AnimationEditorTick_Takeoff(int ticksPassed)
		{
			ticksPassedVertical = ticksPassed.Take(LaunchProperties_VTOL.maxTicksVertical, out int remaining);
			return base.AnimationEditorTick_Takeoff(remaining);
		}

		protected override int AnimationEditorTick_Landing(int ticksPassed)
		{
			ticksPassed = base.AnimationEditorTick_Landing(ticksPassed);
			ticksPassedVertical = ticksPassed.Take(LandingProperties_VTOL.maxTicksVertical, out int remaining);
			return remaining;
		}

		protected override (Vector3 drawPos, float rotation) AnimateLanding(Vector3 drawPos, float rotation)
		{
			if (!LandingProperties_VTOL.rotationVerticalCurve.NullOrEmpty())
			{
				rotation += LandingProperties_VTOL.rotationVerticalCurve.Evaluate(TimeInAnimationVTOL);
			}
			if (!LandingProperties_VTOL.zPositionVerticalCurve.NullOrEmpty())
			{
				drawPos.z += LandingProperties_VTOL.zPositionVerticalCurve.Evaluate(TimeInAnimationVTOL);
			}
			if (!LandingProperties_VTOL.xPositionVerticalCurve.NullOrEmpty())
			{
				drawPos.x += LandingProperties_VTOL.xPositionVerticalCurve.Evaluate(TimeInAnimationVTOL);
			}
			if (!LandingProperties_VTOL.offsetVerticalCurve.NullOrEmpty())
			{
				Vector2 offset = LandingProperties_VTOL.offsetVerticalCurve.EvaluateT(TimeInAnimationVTOL);
				drawPos += new Vector3(offset.x, 0, offset.y);
			}
			return base.AnimateLanding(drawPos, rotation);
		}

		protected override (Vector3 drawPos, float rotation) AnimateTakeoff(Vector3 drawPos, float rotation)
		{
			if (!LaunchProperties_VTOL.rotationVerticalCurve.NullOrEmpty())
			{
				rotation += LaunchProperties_VTOL.rotationVerticalCurve.Evaluate(TimeInAnimationVTOL);
			}
			if (!LaunchProperties_VTOL.zPositionVerticalCurve.NullOrEmpty())
			{
				drawPos.z += LaunchProperties_VTOL.zPositionVerticalCurve.Evaluate(TimeInAnimationVTOL);
			}
			if (!LaunchProperties_VTOL.xPositionVerticalCurve.NullOrEmpty())
			{
				drawPos.x += LaunchProperties_VTOL.xPositionVerticalCurve.Evaluate(TimeInAnimationVTOL);
			}
			if (!LaunchProperties_VTOL.offsetVerticalCurve.NullOrEmpty())
			{
				Vector2 offset = LaunchProperties_VTOL.offsetVerticalCurve.EvaluateT(TimeInAnimationVTOL);
				drawPos += new Vector3(offset.x, 0, offset.y);
			}
			return base.AnimateTakeoff(drawPos, rotation);
		}

		protected override void TickMotes()
		{
			FleckData fleckData = CurAnimationProperties_Vertical.fleckDataVertical;
			if (fleckData != null && (fleckData.runOutOfStep || (TimeInAnimationVTOL > 0 && TimeInAnimationVTOL < 1)))
			{
				effectsToThrowVertical = TryThrowFleck(fleckData, TimeInAnimationVTOL, effectsToThrowVertical);
			}
			base.TickMotes();
		}

		protected override void TickLanding()
		{
			if (ticksPassed < landingProperties.maxTicks)
			{
				base.TickLanding();
			}
			else
			{
				ticksPassedVertical++;
				TickMotes();
			}
		}

		protected override void TickTakeoff()
		{
			if (ticksPassedVertical >= LaunchProperties_VTOL.maxTicksVertical)
			{
				base.TickTakeoff();
			}
			else
			{
				ticksPassedVertical++;
				TickMotes();
			}
		}

		protected override void PreAnimationSetup()
		{
			base.PreAnimationSetup();
			ticksPassedVertical = 0;
			effectsToThrowVertical = 0;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref ticksPassedVertical, nameof(ticksPassedVertical), 0);
			Scribe_Values.Look(ref effectsToThrowVertical, nameof(effectsToThrowVertical), 0);
		}
	}
}
