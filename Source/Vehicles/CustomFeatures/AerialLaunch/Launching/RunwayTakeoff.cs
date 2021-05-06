using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using Vehicles.AI;

namespace Vehicles
{
	public class RunwayTakeoff : LaunchProtocol
	{
		private const float RunwayTakeoffRatio = 0.75f;
		private const int MaxTicksPassed = 500;

		public int minRunwayLength;
		public float maxRunwayAcceleration;
		public List<ThingCategory> avoidThings;

		/* Optional Animation Tweaks */
		public float liftRate = 0.075f;
		public float maxLiftAngle = 15f;
		public float accelGrowth = 2.5f;
		public float accelScale = 100f;
		/* --------------- */

		private IntVec3 startCell;
		private TerrainDef startTerrain;
		private bool liftOff;

		private float angleLifted;
		private float acceleration;
		private int ticksWhenLanded;

		public RunwayTakeoff()
		{
		}

		public RunwayTakeoff(RunwayTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
		{
			minRunwayLength = reference.minRunwayLength;
			maxRunwayAcceleration = reference.maxRunwayAcceleration;
			avoidThings = reference.avoidThings;

			liftRate = reference.liftRate;
			maxLiftAngle = reference.maxLiftAngle;
			accelGrowth = reference.accelGrowth ;
			accelScale = reference.accelScale;

			if (maxRunwayAcceleration <= 0)
			{
				maxRunwayAcceleration = vehicle.ActualMoveSpeed * 5;
			}
		}

		public override string FailLaunchMessage => "RunwayNotValid".Translate();

		public override bool CanLaunchNow
		{
			get
			{
				Rot4 rot = vehicle.Rotation;
				startCell = vehicle.Position;
				startTerrain = vehicle.Map.terrainGrid.TerrainAt(startCell);
				float costPerCell = Vehicle_PathFollower.CostToMoveIntoCell(vehicle, startCell);
				liftOff = false;

				angleLifted = 0;
				acceleration = 0;

				IntVec3 directional = new IntVec3(0, 0, 0);
				switch (rot.AsInt)
				{
					case 0:
						directional.z = 1;
						break;
					case 1:
						directional.x = 1;
						break;
					case 2:
						directional.z = -1;
						break;
					case 3:
						directional.x = -1;
						break;
				}

				for (int i = 0; i <= minRunwayLength; i++)
				{
					IntVec3 cell = startCell + (directional * i);
					if (!cell.InBounds(vehicle.Map))
					{
						return false;
					}
					if (vehicle.Map.terrainGrid.TerrainAt(cell) != startTerrain)
					{
						return false;
					}
					if (vehicle.Map.thingGrid.ThingsAt(cell).NotNullAndAny(t => t != vehicle && ((avoidThings?.Contains(t.def.category) ?? false) || t.def.Fillage > FillCategory.None)))
					{
						return false;
					}
				}
				return true;
			}
		}

		public override Command_Action LaunchCommand
		{
			get
			{
				Command_Highlight runwayTakeoff = new Command_Highlight
				{
					defaultLabel = "CommandLaunchGroup".Translate(),
					defaultDesc = "CommandLaunchGroupDesc".Translate(),
					icon = VehicleTex.LaunchCommandTex,
					alsoClickIfOtherInGroupClicked = false,
					map = vehicle.Map,
					highlightCells = RunwayCells(vehicle.Position, vehicle.Rotation).ToList(),
					validator = ValidCell,
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
				return runwayTakeoff;
			}
		}

		public override bool FinishedTakeoff(VehicleSkyfaller skyfaller)
		{
			return !drawPos.InBounds(skyfaller.Map) && drawPos != Vector3.zero;
		}

		public override bool FinishedLanding(VehicleSkyfaller skyfaller)
		{
			return (!drawPos.InBounds(skyfaller.Map) && !liftOff) || (drawPos.InBounds(skyfaller.Map) && acceleration <= 0.01f);
		}

		public override Vector3 AnimateLanding(float layer, bool flip)
		{
			float angle = 0f;
			if (landingProperties?.angleCurve != null)
			{
				angle = landingProperties.angleCurve.Evaluate(ticksPassed);
			}
			if (landingProperties?.rotationCurve != null)
			{
				angle += landingProperties.rotationCurve.Evaluate(ticksPassed);
			}
			if (landingProperties?.xPositionCurve != null)
			{
				drawPos.x += landingProperties.xPositionCurve.Evaluate(ticksPassed);
			}
			if (landingProperties?.zPositionCurve != null)
			{
				drawPos.z += landingProperties.zPositionCurve.Evaluate(ticksPassed);
			}
			drawPos.y = layer;
			vehicle.DrawAt(drawPos, angle, flip);
			return drawPos;
		}

		public override Vector3 AnimateTakeoff(float layer, bool flip)
		{
			float angle = 0f;
			if (launchProperties?.angleCurve != null)
			{
				angle = launchProperties.angleCurve.Evaluate(ticksPassed);
			}
			if (launchProperties?.rotationCurve != null)
			{
				angle += launchProperties.rotationCurve.Evaluate(ticksPassed);
			}
			if (launchProperties?.xPositionCurve != null)
			{
				drawPos.x += launchProperties.xPositionCurve.Evaluate(ticksPassed);
			}
			if (launchProperties?.zPositionCurve != null)
			{
				drawPos.z += launchProperties.zPositionCurve.Evaluate(ticksPassed);
			}
			drawPos.y = layer;
			vehicle.DrawAt(drawPos, angle, flip);
			return drawPos;
		}

		public override void SetPositionArriving(Vector3 pos, Rot4 rot, Map map)
		{
			switch (rot.AsInt)
			{
				case 0:
					pos.z = -1;
					break;
				case 1:
					pos.z += pos.x * Mathf.Tan(5f * Mathf.Deg2Rad);
					pos.x = -1;
					break;
				case 2:
					pos.z = map.Size.z + 1;
					break;
				case 3:
					pos.z += (map.Size.x - pos.x) * Mathf.Tan(5f * Mathf.Deg2Rad);
					pos.x = map.Size.x + 1;
					break;
			}
			vehicle.Rotation = rot;
			liftOff = true;
			angleLifted = 10;
			ticksWhenLanded = 0;
			base.SetPositionArriving(pos, rot, map);
		}

		public override void PreAnimationSetup()
		{
			base.PreAnimationSetup();
			angleLifted = 0f;
		}

		public override void DrawLandingTarget(IntVec3 cell, Rot4 rot)
		{
			List<IntVec3> cells = RunwayCells(cell, rot).ToList();
			Color highlightColor = cells.Any(c => !ValidCell(Find.CurrentMap, c)) ? Color.red : Color.white;
			GenDraw.DrawFieldEdges(cells, highlightColor);
		}

		public override IEnumerable<FloatMenuOption> GetFloatMenuOptionsAt(int tile)
		{
			if (Find.WorldObjects.MapParentAt(tile) is MapParent parent && CanLandInSpecificCell(parent))
			{
				foreach (LaunchProtocol protocol in vehicle.CompVehicleLauncher.launchProtocols)
				{
					yield return new FloatMenuOption("LandInExistingMap".Translate(vehicle.Label), delegate()
					{
						Current.Game.CurrentMap = parent.Map;
						CameraJumper.TryHideWorld();
						Targeters.LandingTargeter.BeginTargeting(vehicle, protocol, delegate (LocalTargetInfo target, Rot4 rot)
						{
							vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_LandSpecificCell(vehicle, parent, tile, this, target.Cell, rot));
						}, null, null, null, vehicle.VehicleDef.rotatable && protocol.landingProperties.forcedRotation is null);
					}, MenuOptionPriority.Default, null, null, 0f, null, null);
				}
				
			}
			else if (Find.WorldObjects.SettlementAt(tile) is Settlement settlement)
			{
				if (settlement.Faction.def.techLevel <= TechLevel.Industrial)
				{
					yield return new FloatMenuOption("LandVehicleHere".Translate(), delegate ()
					{
						vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_FormVehicleCaravan(vehicle));
					}, MenuOptionPriority.Default, null, null, 0f, null, null);
				}
			}
			if (Find.WorldObjects.AnyMapParentAt(tile))
			{
				yield return new FloatMenuOption("AirstrikeHere".Translate(), delegate ()
				{
					vehicle.CompVehicleLauncher.TryLaunch(tile, new AerialVehicleArrivalAction_FormVehicleCaravan(vehicle));
				}, MenuOptionPriority.Default, null, null, 0f, null, null);
			}
		}

		public override void StartChoosingDestination()
		{
			base.StartChoosingDestination();
			CameraJumper.TryJump(CameraJumper.GetWorldTarget(vehicle));
			Find.WorldSelector.ClearSelection();
			int tile = vehicle.Map.Tile;
			Targeters.LaunchTargeter.BeginTargeting(vehicle, new Func<GlobalTargetInfo, float, bool>(ChoseWorldTarget), vehicle.Map.Tile, true, VehicleTex.TargeterMouseAttachment, true, null, 
				(GlobalTargetInfo target, List<int> path, float fuelCost) => TargetingLabelGetter(target, tile, path, fuelCost));
		}

		public override bool ChoseWorldTarget(GlobalTargetInfo target, Vector3 pos, float fuelCost, Action<int, AerialVehicleArrivalAction, bool> launchAction)
		{
			if (Find.WorldObjects.SettlementAt(target.Tile) is Settlement settlement)
			{
				if (!settlement.Faction.AllyOrNeutralTo(Faction.OfPlayer))
				{
					Messages.Message("VehicleRunwayCannotLandEnemy".Translate(), MessageTypeDefOf.RejectInput);
					return false;
				}
				else if (settlement.Faction.def.techLevel < TechLevel.Industrial)
				{
					Messages.Message("VehicleRunwayCannotLandTechLevel".Translate(), MessageTypeDefOf.RejectInput);
					return false;
				}
			}
			else
			{
				Messages.Message("VehicleRunwayCannotLand".Translate(), MessageTypeDefOf.RejectInput);
				return false;
			}
			//Add bombing
			return base.ChoseWorldTarget(target, pos, fuelCost, launchAction);
		}

		private IEnumerable<IntVec3> RunwayCells(IntVec3 cell, Rot4 rot)
		{
			IntVec3 directional = new IntVec3(0, 0, 0);
			switch (rot.AsInt)
			{
				case 0:
					directional.z = 1;
					break;
				case 1:
					directional.x = 1;
					break;
				case 2:
					directional.z = -1;
					break;
				case 3:
					directional.x = -1;
					break;
			}
			for (int i = 0; i <= minRunwayLength; i++)
			{
				yield return cell + (directional * i);
			}
		}

		private bool ValidCell(Map map, IntVec3 cell)
		{
			if (!cell.InBounds(map))
			{
				return false;
			}
			TerrainDef terrainCell = map.terrainGrid.TerrainAt(cell);
			if (terrainCell != startTerrain)
			{
				return false;
			}
			if (vehicle.VehicleDef.properties.customTerrainCosts.TryGetValue(terrainCell, out int cost))
			{
				if (cost > 1)
				{
					return false;
				}
			}
			else
			{
				if (map.pathGrid.PerceivedPathCostAt(cell) > 1)
				{
					return false;
				}
			}
			if (map.thingGrid.ThingsAt(cell).NotNullAndAny(t => t != vehicle && ((avoidThings?.Contains(t.def.category) ?? false) || t.def.Fillage > FillCategory.None)))
			{
				return false;
			}
			return true;
		}

		private void TakeStepLanding()
		{
			if (liftOff)
			{
				float x = acceleration;
				float y = -acceleration * (float)Mathf.Sin(4f * Mathf.Deg2Rad);
				switch (vehicle.Rotation.AsInt)
				{
					case 0:
						drawPos.z += acceleration;
						break;
					case 1:
						drawPos.x += x;
						drawPos.z += y;
						break;
					case 2:
						drawPos.z -= acceleration;
						break;
					case 3:
						drawPos.x -= x;
						drawPos.z += y;
						break;
				}
			}
			else
			{
				if (angleLifted > 0)
				{
					angleLifted -= liftRate;
				}
				else
				{
					angleLifted = 0;
				}
				switch (vehicle.Rotation.AsInt)
				{
					case 0:
						drawPos.z += acceleration;
						break;
					case 1:
						drawPos.x += acceleration;
						break;
					case 2:
						drawPos.z -= acceleration;
						break;
					case 3:
						drawPos.x -= acceleration;
						break;
				}
			}
			if (vehicle.Rotation != Rot4.North && drawPos.z - startCell.z <= 0.1f || vehicle.Rotation == Rot4.North && startCell.z - drawPos.z <= 0.1f)
			{
				liftOff = false;
				if (vehicle.Rotation.IsHorizontal)
				{
					drawPos.z = startCell.z;
				}
				else
				{
					drawPos.x = startCell.x;
				}
			}
		}

		private void TakeStepTakeoff()
		{
			if (liftOff)
			{
				if (angleLifted < maxLiftAngle)
				{
					angleLifted += liftRate;
				}
				float x = acceleration;
				float y = acceleration * (float)Math.Sin(angleLifted * Mathf.Deg2Rad);
				switch (vehicle.Rotation.AsInt)
				{
					case 0:
						drawPos.z += acceleration;
						break;
					case 1:
						drawPos.x += x;
						drawPos.z += y;
						break;
					case 2:
						drawPos.z -= acceleration;
						break;
					case 3:
						drawPos.x -= x;
						drawPos.z += y;
						break;
				}
			}
			else
			{
				switch (vehicle.Rotation.AsInt)
				{
					case 0:
						drawPos.z += acceleration;
						break;
					case 1:
						drawPos.x += acceleration;
						break;
					case 2:
						drawPos.z -= acceleration;
						break;
					case 3:
						drawPos.x -= acceleration;
						break;
				}
			}
			if (Math.Abs(drawPos.x - startCell.x) >= (minRunwayLength * RunwayTakeoffRatio) || Math.Abs(drawPos.z - startCell.z) >= (minRunwayLength * RunwayTakeoffRatio))
			{
				liftOff = true;
			}
		}

		public double CalculateAcceleration(int ticks)
		{
			//Weibull (stretched exponential) function
			//y = 1 - exp ^ (-(x / accelScale) ^ accelGrowth)
			//https://www.desmos.com/calculator/t4it3keokx
			return 1 - Math.Exp(-Math.Pow((ticks / (accelScale)), accelGrowth));
		}

		public double CalculateDeceleration(int ticks)
		{
			//Weibull (stretched exponential) function reflected
			//y = exp ^ (-(x / accelScale) ^ accelGrowth)
			//https://www.desmos.com/calculator/t4it3keokx
			return Math.Exp(-Math.Pow((ticks / (accelScale)), accelGrowth)); 
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref minRunwayLength, "minRunwayLength");
			Scribe_Values.Look(ref maxRunwayAcceleration, "maxRunwayAcceleration");
			Scribe_Collections.Look(ref avoidThings, "avoidThings");

			Scribe_Values.Look(ref liftRate, "liftRate");
			Scribe_Values.Look(ref maxLiftAngle, "maxLiftAngle");
			Scribe_Values.Look(ref accelGrowth, "accelGrowth");
			Scribe_Values.Look(ref accelScale, "accelScale");

			Scribe_Values.Look(ref startCell, "startCell");
			Scribe_Defs.Look(ref startTerrain, "startTerrain");
			Scribe_Values.Look(ref liftOff, "liftOff");

			Scribe_Values.Look(ref angleLifted, "angleLifted");
			Scribe_Values.Look(ref acceleration, "acceleration");
			Scribe_Values.Look(ref ticksWhenLanded, "ticksWhenLanded");
		}
	}
}
