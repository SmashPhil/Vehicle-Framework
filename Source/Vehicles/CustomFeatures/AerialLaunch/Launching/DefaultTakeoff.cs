using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class DefaultTakeoff : LaunchProtocol
	{
		public SkyfallerMovementType movementType;
		protected float angle;
		protected float rotation;

		protected List<MoteInfo> motes = new List<MoteInfo>();

		public DefaultTakeoff()
		{
		}

		public DefaultTakeoff(DefaultTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
		{
			movementType = reference.movementType;
			angle = reference.angle;
			rotation = reference.rotation;
			motes = reference.motes;
		}

		public override string FailLaunchMessage => "SkyfallerLaunchNotValid".Translate();

		public override bool CanLaunchNow
		{
			get
			{
				if (vehicle.Map != null)
				{
					return !vehicle.Position.Roofed(vehicle.Map);
				}
				return true;
			}
		}

		public override Vector3 DrawPos
		{
			get
			{
				switch (movementType)
				{
					case SkyfallerMovementType.Accelerate:
						return SkyfallerDrawPosUtility.DrawPos_Accelerate(drawPos, ticksPassed, angle, CurrentSpeed);
					case SkyfallerMovementType.ConstantSpeed:
						return SkyfallerDrawPosUtility.DrawPos_ConstantSpeed(drawPos, ticksPassed, angle, CurrentSpeed);
					case SkyfallerMovementType.Decelerate:
						return SkyfallerDrawPosUtility.DrawPos_Decelerate(drawPos, ticksPassed, angle, CurrentSpeed);
					default:
						Log.ErrorOnce("SkyfallerMovementType not handled: " + movementType, vehicle.thingIDNumber ^ 1948576711);
						return SkyfallerDrawPosUtility.DrawPos_Accelerate(drawPos, ticksPassed, angle, CurrentSpeed);
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

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptionsAt(int tile)
		{
			if (AerialVehicleArrivalAction_FormVehicleCaravan.CanFormCaravanAt(vehicle, tile) && !Find.WorldObjects.AnySettlementBaseAt(tile) && !Find.WorldObjects.AnySiteAt(tile))
			{
				yield return new FloatMenuOption("FormCaravanHere".Translate(), delegate ()
				{
					if (vehicle.Spawned)
					{
						vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_FormVehicleCaravan(vehicle));
					}
					else
					{
						AerialVehicleInFlight aerial = VehicleWorldObjectsHolder.Instance.AerialVehicleObject(vehicle);
						aerial.OrderFlyToTiles(LaunchTargeter.FlightPath, aerial.DrawPos, new AerialVehicleArrivalAction_FormVehicleCaravan(vehicle));
					}
				}, MenuOptionPriority.Default, null, null, 0f, null, null);
			}
			else if (Find.WorldObjects.MapParentAt(tile) is MapParent parent)
			{
				if (CanLandInSpecificCell(parent))
				{
					yield return new FloatMenuOption("LandInExistingMap".Translate(vehicle.Label), delegate ()
					{
						Current.Game.CurrentMap = parent.Map;
						CameraJumper.TryHideWorld();
						LandingTargeter.Instance.BeginTargeting(vehicle, this, delegate (LocalTargetInfo target, Rot4 rot)
						{
							if (vehicle.Spawned)
							{
								vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_LandSpecificCell(vehicle, parent, tile, this, target.Cell, rot));
							}
							else
							{
								AerialVehicleInFlight aerial = VehicleWorldObjectsHolder.Instance.AerialVehicleObject(vehicle);
								if (aerial is null)
								{
									Log.Error($"Attempted to launch into existing map where CurrentMap is null and no AerialVehicle with {vehicle.Label} exists.");
									return;
								}
								aerial.arrivalAction = new AerialVehicleArrivalAction_LandSpecificCell(vehicle, parent, tile, this, target.Cell, rot);
								aerial.OrderFlyToTiles(LaunchTargeter.FlightPath, aerial.DrawPos, new AerialVehicleArrivalAction_LandSpecificCell(vehicle, parent, tile, this, target.Cell, rot));
								vehicle.CompVehicleLauncher.inFlight = true;
								CameraJumper.TryShowWorld();
							}
						}, null, null, null, vehicle.VehicleDef.rotatable && landingProperties.forcedRotation is null);
					}, MenuOptionPriority.Default, null, null, 0f, null, null);
				}
				if (vehicle.CompVehicleLauncher.ControlInFlight)
				{
					yield return MapHelper.ReconFloatMenuOption(vehicle, parent);
				}
				if (vehicle.CompVehicleLauncher.ControlInFlight && vehicle.CompVehicleTurrets != null) //REDO - strafe specific properties
				{
					yield return new FloatMenuOption("VehicleStrafeRun".Translate(), delegate ()
					{
						if (vehicle.Spawned)
						{
							LaunchTargeter.ContinueTargeting(vehicle, new Func<GlobalTargetInfo, float, bool>(ChoseWorldTarget), vehicle.Map.Tile, true, VehicleTex.TargeterMouseAttachment, true, null,
								(GlobalTargetInfo target, List<FlightNode> path, float fuelCost) => TargetingLabelGetter(target, tile, path, fuelCost));
						}
						else
						{
							AerialVehicleInFlight aerialVehicle = vehicle.GetAerialVehicle();
							if (aerialVehicle is null)
							{
								Log.Error($"Unable to launch strafe run. AerialVehicle is null and {vehicle.LabelCap} is not spawned.");
								return;
							}
							LaunchTargeter.Instance.ContinueTargeting(vehicle, new Func<GlobalTargetInfo, float, bool>(aerialVehicle.ChoseTargetOnMap), aerialVehicle, true, VehicleTex.TargeterMouseAttachment, false, null,
								(GlobalTargetInfo target, List<FlightNode> path, float fuelCost) => vehicle.CompVehicleLauncher.launchProtocol.TargetingLabelGetter(target, aerialVehicle.Tile, path, fuelCost));
						}
						CameraJumper.TryShowWorld();
						LaunchTargeter.Instance.RegisterActionOnTile(tile, new AerialVehicleArrivalAction_StrafeMap(vehicle, parent));
					}, MenuOptionPriority.Default, null, null, 0f, null, null);
				}
			}
			if (Find.WorldObjects.SettlementAt(tile) is Settlement settlement)
			{
				if (settlement.Faction.def.techLevel <= TechLevel.Industrial)
				{
					yield return new FloatMenuOption("LandVehicleHere".Translate(), delegate ()
					{
						if (vehicle.Spawned)
						{
							vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_FormVehicleCaravan(vehicle));
						}
						else
						{
							AerialVehicleInFlight aerial = VehicleWorldObjectsHolder.Instance.AerialVehicleObject(vehicle);
							aerial.OrderFlyToTiles(LaunchTargeter.FlightPath, aerial.DrawPos, new AerialVehicleArrivalAction_VisitSettlement(vehicle, settlement));
						}
					}, MenuOptionPriority.Default, null, null, 0f, null, null);
				}

                if (AerialVehicleArrivalAction_Trade.CanTradeWith(vehicle, settlement))
                {
                    yield return new FloatMenuOption("TradeWith".Translate(settlement.Label), delegate()
                    {
                        if (vehicle.Spawned)
                        {
                            vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_Trade(vehicle, settlement));
                        }
                        else
                        {
                            AerialVehicleInFlight aerial = VehicleWorldObjectsHolder.Instance.AerialVehicleObject(vehicle);
                            aerial.OrderFlyToTiles(LaunchTargeter.FlightPath, aerial.DrawPos, new AerialVehicleArrivalAction_Trade(vehicle, settlement));
                        }
                    });
                }
				foreach (FloatMenuOption option in AerialVehicleArrivalAction_AttackSettlement.GetFloatMenuOptions(vehicle, this, settlement))
				{
					yield return option;
				}
			}
		}

		public override void StartChoosingDestination()
		{
			CameraJumper.TryJump(CameraJumper.GetWorldTarget(vehicle));
			Find.WorldSelector.ClearSelection();
			int tile = vehicle.Map.Tile;
			LaunchTargeter.BeginTargeting(vehicle, new Func<GlobalTargetInfo, float, bool>(ChoseWorldTarget), vehicle.Map.Tile, true, VehicleTex.TargeterMouseAttachment, true, null, 
				(GlobalTargetInfo target, List<FlightNode> path, float fuelCost) => TargetingLabelGetter(target, tile, path, fuelCost));
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref movementType, "movementType");
			Scribe_Values.Look(ref angle, "angle");
			Scribe_Values.Look(ref rotation, "rotation");

			Scribe_Collections.Look(ref motes, "motes", LookMode.Deep);
		}
	}
}
