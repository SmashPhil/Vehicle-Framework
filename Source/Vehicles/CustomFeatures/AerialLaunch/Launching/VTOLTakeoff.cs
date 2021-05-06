using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	public class VTOLTakeoff : DefaultTakeoff
	{
		public int ticksVertical;
		public SimpleCurve verticalLaunchCurve;
		public SimpleCurve verticalLandingCurve;
		private int ticksPassedVertical;

		public VTOLTakeoff()
		{
		}

		public VTOLTakeoff(VTOLTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
		{
			ticksVertical = reference.ticksVertical;
			verticalLaunchCurve = reference.verticalLaunchCurve;
			verticalLandingCurve = reference.verticalLandingCurve;
			ticksPassedVertical = 0;
		}

		public override string FailLaunchMessage => "SkyfallerLaunchNotValid".Translate();

		public virtual float TimeInVTOL
		{
			get
			{
				if (landing)
				{
					if (landingProperties.reversed)
					{
						return (float)ticksPassedVertical / landingProperties.maxTicks;
					}
					return 1f - (float)ticksPassedVertical / landingProperties.maxTicks;
				}
				else
				{
					if (launchProperties.reversed)
					{
						return (float)ticksPassedVertical / launchProperties.maxTicks;
					}
					return 1f - (float)ticksPassedVertical / launchProperties.maxTicks;
				}
			}
		}

		public override Vector3 DrawPos
		{
			get
			{
				Vector3 vDrawPos = drawPos;
				vDrawPos.z += landing ? verticalLandingCurve.Evaluate(TimeInVTOL) : verticalLaunchCurve.Evaluate(TimeInVTOL);
				if (ticksPassedVertical >= ticksVertical)
				{
					switch (movementType)
					{
						case SkyfallerMovementType.Accelerate:
							return SkyfallerDrawPosUtility.DrawPos_Accelerate(vDrawPos, ticksPassed, angle, LaunchSpeed);
						case SkyfallerMovementType.ConstantSpeed:
							return SkyfallerDrawPosUtility.DrawPos_ConstantSpeed(vDrawPos, ticksPassed, angle, LaunchSpeed);
						case SkyfallerMovementType.Decelerate:
							return SkyfallerDrawPosUtility.DrawPos_Decelerate(vDrawPos, ticksPassed, angle, LaunchSpeed);
						default:
							Log.ErrorOnce("SkyfallerMovementType not handled: " + movementType, vehicle.thingIDNumber);
							return SkyfallerDrawPosUtility.DrawPos_Accelerate(vDrawPos, ticksPassed, angle, LaunchSpeed);
					}
				}
				return vDrawPos;
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
			adjustedDrawPos.y = layer;
			vehicle.DrawAt(adjustedDrawPos, rotation, flip);
			return adjustedDrawPos;
		}

		protected override void TickLanding()
		{
			if (ticksPassedVertical >= ticksVertical)
			{
				base.TickLanding();
			}
			else
			{
				ticksPassedVertical++;
			}
		}

		protected override void TickTakeoff()
		{
			if (ticksPassedVertical >= ticksVertical)
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

		public override void ResolveProperties(LaunchProtocol reference)
		{
			base.ResolveProperties(reference);
			VTOLTakeoff vtolReference = reference as VTOLTakeoff;
			verticalLaunchCurve = vtolReference.verticalLaunchCurve;
			verticalLandingCurve = vtolReference.verticalLandingCurve;
			ticksVertical = vtolReference.ticksVertical;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref ticksPassedVertical, "ticksPassedVertical");
		}
	}
}
