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
		public SimpleCurve verticalLaunchCurve;
		public SimpleCurve verticalLandingCurve;
		public SimpleCurve verticalLaunchRotationCurve;
		public SimpleCurve verticalLandingRotationCurve;
		protected int ticksPassedVertical;

		public VTOLTakeoff()
		{
		}

		public VTOLTakeoff(VTOLTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
		{
			verticalLaunchCurve = reference.verticalLaunchCurve;
			verticalLandingCurve = reference.verticalLandingCurve;
			verticalLaunchRotationCurve = reference.verticalLaunchRotationCurve;
			verticalLandingRotationCurve = reference.verticalLandingRotationCurve;
		}

		public VerticalProtocolProperties VerticalLaunchProperties => launchProperties as VerticalProtocolProperties;
		public VerticalProtocolProperties VerticalLandingProperties => landingProperties as VerticalProtocolProperties;

		public override string FailLaunchMessage => "SkyfallerLaunchNotValid".Translate();

		public virtual float TimeInLaunchVTOL => (float)ticksPassedVertical / VerticalLaunchProperties.ticksVertical;

		public virtual float TimeInLandingVTOL => (float)ticksPassedVertical / VerticalLandingProperties.ticksVertical;

		public override Vector3 DrawPos
		{
			get
			{
				Vector3 vDrawPos = drawPos;
				if (landing && verticalLandingCurve != null)
				{
					vDrawPos.z += verticalLandingCurve.Evaluate(TimeInLandingVTOL);
				}
				else if (verticalLaunchCurve != null)
				{
					vDrawPos.z += verticalLaunchCurve.Evaluate(TimeInLaunchVTOL);
				}
				switch (movementType)
				{
					case SkyfallerMovementType.Accelerate:
						return SkyfallerDrawPosUtility.DrawPos_Accelerate(vDrawPos, ticksPassed, angle, CurrentSpeed);
					case SkyfallerMovementType.ConstantSpeed:
						return SkyfallerDrawPosUtility.DrawPos_ConstantSpeed(vDrawPos, ticksPassed, angle, CurrentSpeed);
					case SkyfallerMovementType.Decelerate:
						return SkyfallerDrawPosUtility.DrawPos_Decelerate(vDrawPos, ticksPassed, angle, CurrentSpeed);
					default:
						Log.ErrorOnce("SkyfallerMovementType not handled: " + movementType, vehicle.thingIDNumber);
						return SkyfallerDrawPosUtility.DrawPos_Accelerate(vDrawPos, ticksPassed, angle, CurrentSpeed);
				}
			}
		}

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

		public override bool FinishedLanding(VehicleSkyfaller skyfaller)
		{
			return ticksPassedVertical <= 0 && base.FinishedLanding(skyfaller);
		}

		public override bool FinishedTakeoff(VehicleSkyfaller skyfaller)
		{
			return ticksPassedVertical >= VerticalLaunchProperties.ticksVertical && base.FinishedTakeoff(skyfaller);
		}

		public override Vector3 AnimateLanding(float layer, bool flip)
		{
			Vector3 adjustedDrawPos = DrawPos;
			if (landingProperties?.angleCurve != null)
			{
				angle = landingProperties.angleCurve.Evaluate(TimeInAnimation);
			}
			if (landingProperties?.rotationCurve != null)
			{
				rotation = landingProperties.rotationCurve.Evaluate(TimeInAnimation);
			}
			if (landingProperties?.xPositionCurve != null)
			{
				adjustedDrawPos.x += landingProperties.xPositionCurve.Evaluate(TimeInAnimation);
			}
			if (landingProperties?.zPositionCurve != null)
			{
				adjustedDrawPos.z += landingProperties.zPositionCurve.Evaluate(TimeInAnimation);
			}

			if (ticksPassedVertical < VerticalLandingProperties.ticksVertical && verticalLandingRotationCurve != null)
			{
				rotation = verticalLandingRotationCurve.Evaluate(TimeInLandingVTOL);
			}

			adjustedDrawPos.y = layer;
			vehicle.DrawAt(adjustedDrawPos, rotation, flip);
			return adjustedDrawPos;
		}

		public override Vector3 AnimateTakeoff(float layer, bool flip)
		{
			Vector3 adjustedDrawPos = DrawPos;
			if (launchProperties?.angleCurve != null)
			{
				angle = launchProperties.angleCurve.Evaluate(TimeInAnimation);
			}
			if (launchProperties?.rotationCurve != null)
			{
				rotation = launchProperties.rotationCurve.Evaluate(TimeInAnimation);
			}
			if (launchProperties?.xPositionCurve != null)
			{
				adjustedDrawPos.x += launchProperties.xPositionCurve.Evaluate(TimeInAnimation);
			}
			if (launchProperties?.zPositionCurve != null)
			{
				adjustedDrawPos.z += launchProperties.zPositionCurve.Evaluate(TimeInAnimation);
			}

			if (ticksPassedVertical < VerticalLaunchProperties.ticksVertical && verticalLaunchRotationCurve != null)
			{
				rotation = verticalLaunchRotationCurve.Evaluate(TimeInLaunchVTOL);
			}

			adjustedDrawPos.y = layer;
			vehicle.DrawAt(adjustedDrawPos, rotation, flip);
			return adjustedDrawPos;
		}

		protected override void TickLanding()
		{
			if (ticksPassed > 0)
			{
				base.TickLanding();
			}
			else
			{
				ticksPassedVertical--;
			}
		}

		protected override void TickTakeoff()
		{
			if (ticksPassedVertical >= VerticalLaunchProperties.ticksVertical)
			{
				base.TickTakeoff();
			}
			else
			{
				ticksPassedVertical++;
				if (!motes.NullOrEmpty())
				{
					Rand.PushState();
					foreach (MoteInfo mote in motes)
					{
						float randSmokeX = drawPos.x + Rand.Range(-0.1f, 0.1f);
						float smokeZOffset = vehicle.VehicleDef.Size.z / 2;
						Vector3 vector = new Vector3(randSmokeX, drawPos.y, drawPos.z - smokeZOffset);
						ThrowMoteLong(mote.moteDef, vector, currentMap, mote.size.RandomInRange, mote.angle.RandomInRange, mote.speed.RandomInRange);
					}
					Rand.PopState();
				}
			}
		}

		protected override void PreAnimationSetup()
		{
			base.PreAnimationSetup();
			ticksPassedVertical = landing ? VerticalLandingProperties.ticksVertical : 0;
		}

		public override void ResolveProperties(LaunchProtocol reference)
		{
			base.ResolveProperties(reference);
			VTOLTakeoff vtolReference = reference as VTOLTakeoff;
			verticalLaunchCurve = vtolReference.verticalLaunchCurve;
			verticalLandingCurve = vtolReference.verticalLandingCurve;
			verticalLaunchRotationCurve = vtolReference.verticalLaunchRotationCurve;
			verticalLandingRotationCurve = vtolReference.verticalLandingRotationCurve;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref ticksPassedVertical, "ticksPassedVertical");
		}
	}
}
