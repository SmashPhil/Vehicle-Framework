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
	public class DefaultTakeoff : LaunchProtocol
	{
		public DefaultTakeoff()
		{
		}

		public DefaultTakeoff(DefaultTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
		{
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

		protected override (Vector3 drawPos, float rotation) AnimateLanding(Vector3 drawPos, float rotation)
		{
			if (!landingProperties.rotationCurve.NullOrEmpty())
			{
				rotation += landingProperties.rotationCurve.Evaluate(TimeInAnimation);
			}
			if (!landingProperties.xPositionCurve.NullOrEmpty())
			{
				drawPos.x += landingProperties.xPositionCurve.Evaluate(TimeInAnimation);
			}
			if (!landingProperties.zPositionCurve.NullOrEmpty())
			{
				drawPos.z += landingProperties.zPositionCurve.Evaluate(TimeInAnimation);
			}
			if (!landingProperties.offsetCurve.NullOrEmpty())
			{
				Vector2 offset = landingProperties.offsetCurve.EvaluateT(TimeInAnimation);
				drawPos += new Vector3(offset.x, 0, offset.y);
			}
			return base.AnimateLanding(drawPos, rotation);
		}

		protected override (Vector3 drawPos, float rotation) AnimateTakeoff(Vector3 drawPos, float rotation)
		{
			if (!launchProperties.rotationCurve.NullOrEmpty())
			{
				rotation += launchProperties.rotationCurve.Evaluate(TimeInAnimation);
			}
			if (!launchProperties.xPositionCurve.NullOrEmpty())
			{
				drawPos.x += launchProperties.xPositionCurve.Evaluate(TimeInAnimation);
			}
			if (!launchProperties.zPositionCurve.NullOrEmpty())
			{
				drawPos.z += launchProperties.zPositionCurve.Evaluate(TimeInAnimation);
			}
			if (!launchProperties.offsetCurve.NullOrEmpty())
			{
				Vector2 offset = launchProperties.offsetCurve.EvaluateT(TimeInAnimation);
				drawPos += new Vector3(offset.x, 0, offset.y);
			}
			return base.AnimateTakeoff(drawPos, rotation);
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
								vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_LandSpecificCell(vehicle, parent, tile, target.Cell, rot));
							}
							else
							{
								AerialVehicleInFlight aerial = VehicleWorldObjectsHolder.Instance.AerialVehicleObject(vehicle);
								if (aerial is null)
								{
									Log.Error($"Attempted to launch into existing map where CurrentMap is null and no AerialVehicle with {vehicle.Label} exists.");
									return;
								}
								aerial.arrivalAction = new AerialVehicleArrivalAction_LandSpecificCell(vehicle, parent, tile, target.Cell, rot);
								aerial.OrderFlyToTiles(LaunchTargeter.FlightPath, aerial.DrawPos, new AerialVehicleArrivalAction_LandSpecificCell(vehicle, parent, tile, target.Cell, rot));
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
	}
}
