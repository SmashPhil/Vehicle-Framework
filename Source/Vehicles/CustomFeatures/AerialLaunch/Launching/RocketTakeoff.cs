using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace Vehicles
{
	public class RocketTakeoff : DefaultTakeoff
	{
		private float finalAngle;

		protected float rocketTiltRate;
		protected FloatRange thrusterSize;
		protected FloatRange? dustSize;
		protected int burnRadius;
		
		public RocketTakeoff()
		{
		}

		public RocketTakeoff(RocketTakeoff reference, VehiclePawn vehicle) : base(reference, vehicle)
		{
			rocketTiltRate = reference.rocketTiltRate;
			thrusterSize = reference.thrusterSize;
			dustSize = reference.dustSize;
			burnRadius = reference.burnRadius;
		}

		public override Command_ActionHighlighter LaunchCommand
		{
			get
			{
				Command_HighlightRadius skyfallerTakeoff = new Command_HighlightRadius
				{
					defaultLabel = "CommandLaunchGroup".Translate(),
					defaultDesc = "CommandLaunchGroupDesc".Translate(),
					icon = VehicleTex.LaunchCommandTex,
					alsoClickIfOtherInGroupClicked = false,
					radius = VehicleMod.settings.main.burnRadiusOnRockets ? burnRadius : 0,
					center = vehicle.Position,
					color = new Color(1, 0.5f, 0),
					needsLOS = true,
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

		public override bool ChoseWorldTarget(GlobalTargetInfo target, Vector3 pos, Func<GlobalTargetInfo, Vector3, Action<int, AerialVehicleArrivalAction, bool>, bool> validator, 
			Action<int, AerialVehicleArrivalAction, bool> launchAction)
		{
			if (vehicle.Map != null)
			{
				var direction = Find.WorldGrid.GetDirection8WayFromTo(vehicle.Map.Tile, target.Tile);
				switch (direction)
				{
					case Direction8Way.NorthEast:
					case Direction8Way.SouthEast:
					case Direction8Way.East:
						finalAngle = 15f;
						break;
					case Direction8Way.NorthWest:
					case Direction8Way.SouthWest:
					case Direction8Way.West:
						finalAngle = -15f;
						rocketTiltRate *= -1f;
						break;
					default:
						//angle = 0f;
						break;
				}
			}
			return base.ChoseWorldTarget(target, pos, validator, launchAction);
		}

		//public override void DrawAdditionalLandingTextures(Vector3 drawPos)
		//{
		//	if (!LaunchGraphics.NullOrEmpty() /*&& DrawPos.z - drawPos.z > vehicle.VehicleDef.Size.z*/)
		//	{
		//		Rand.PushState();
		//		for (int i = 0; i < LaunchGraphics.Count; i++)
		//		{
		//			Graphic graphic = LaunchGraphics[i];
		//			if (graphic is Graphic_Animate animationGraphic)
		//			{
		//				//animationGraphic.DrawWorkerAnimated(drawPos, Rot4.North, ticksPassed, rotation, true);
		//			}
		//			else
		//			{
		//				//graphic.DrawWorker(drawPos, Rot4.North, null, null, rotation);
		//			}
		//		}
		//		Rand.PopState();
		//	}
		//}

		//public override void DrawAdditionalLaunchTextures(Vector3 drawPos)
		//{
		//	if (!LaunchGraphics.NullOrEmpty() /*&& DrawPos.z - drawPos.z > vehicle.VehicleDef.Size.z*/)
		//	{
		//		Rand.PushState();
		//		for (int i = 0; i < LaunchGraphics.Count; i++)
		//		{
		//			Graphic graphic = LaunchGraphics[i];
		//			if (graphic is Graphic_Animate animationGraphic)
		//			{
		//				//animationGraphic.DrawWorkerAnimated(drawPos, Rot4.North, ticksPassed, rotation, true);
		//			}
		//			else
		//			{
		//				//graphic.DrawWorker(drawPos, Rot4.North, null, null, rotation);
		//			}
		//		}
		//		Rand.PopState();
		//	}
		//}

		protected override void TickLanding()
		{
			base.TickLanding();
			//if (dustSize.HasValue)
			//{
			//	Rand.PushState();
			//	if (DrawPos.z - drawPos.z <= vehicle.VehicleDef.Size.z * 4 && DrawPos.z > drawPos.z)
			//	{
			//		float randX = drawPos.x + Rand.Range(-0.5f, 0.5f);
			//		float zOffset = vehicle.VehicleDef.Size.z / 2;
			//		ThrowRocketExhaustLong(new Vector3(randX, drawPos.y, drawPos.z - zOffset), targetMap, thrusterSize.RandomInRange);
			//		float randSmokeX = drawPos.x + Rand.Range(-0.1f, 0.1f);
			//		float smokeZOffset = vehicle.VehicleDef.Size.z / 1.5f;
			//		ThrowRocketSmokeLong(new Vector3(randSmokeX, drawPos.y, drawPos.z - smokeZOffset), targetMap, dustSize.Value.RandomInRange);
			//		if (ticksPassed % 50 == 0)
			//		{
			//			BurnCells(targetMap);
			//		}
			//	}
			//	if (DrawPos.z - drawPos.z > vehicle.VehicleDef.Size.z * 3)
			//	{
			//		float randX = DrawPos.x + Rand.Range(-0.1f, 0.1f);
			//		float zPos = DrawPos.z - vehicle.VehicleDef.Size.z / 2f;
			//		Vector3 motePos = new Vector3(randX, DrawPos.y, zPos);
			//		ThrowRocketExhaust(motePos, targetMap, 1, Rand.Range(175, 185), 35);
			//	}
			//	else if (DrawPos.z - drawPos.z > vehicle.VehicleDef.Size.z)
			//	{
			//		float randX = DrawPos.x + Rand.Range(-0.1f, 0.1f);
			//		float zPos = DrawPos.z - vehicle.VehicleDef.Size.z / 2f;
			//		Vector3 motePos = new Vector3(randX, DrawPos.y, zPos);
			//		ThrowRocketExhaust(motePos, targetMap, 1, Rand.Range(175, 185), Mathf.Lerp(5, 35, (DrawPos.z - drawPos.z) / (vehicle.VehicleDef.Size.z * 3)));
			//	}
			//	Rand.PopState();
			//}
		}

		protected override void TickTakeoff()
		{
			base.TickTakeoff();
			//if (dustSize.HasValue)
			//{ 
			//	Rand.PushState();
			//	if (DrawPos.z - drawPos.z <= vehicle.VehicleDef.Size.z)
			//	{
			//		float randX = drawPos.x + Rand.Range(-0.5f, 0.5f);
			//		float zOffset = vehicle.VehicleDef.Size.z / 2;
			//		ThrowRocketExhaustLong(new Vector3(randX, drawPos.y, drawPos.z - zOffset), currentMap, thrusterSize.RandomInRange);
			//		float randSmokeX = drawPos.x + Rand.Range(-0.1f, 0.1f);
			//		float smokeZOffset = vehicle.VehicleDef.Size.z / 1.5f;
			//		ThrowRocketSmokeLong(new Vector3(randSmokeX, drawPos.y, drawPos.z - smokeZOffset), currentMap, dustSize.Value.RandomInRange);
			//		if (ticksPassed % 50 == 0)
			//		{
			//			BurnCells(currentMap);
			//		}
			//	}
			//	else
			//	{
			//		float randX = DrawPos.x + Rand.Range(-0.1f, 0.1f);
			//		float zPos = DrawPos.z - vehicle.VehicleDef.Size.z / 2f;
			//		Vector3 motePos = new Vector3(randX, DrawPos.y, zPos);
			//		ThrowRocketExhaust(motePos, currentMap, 1, Rand.Range(0f, 360f), 0.12f);
			//		//if (Math.Abs(angle) <= Math.Abs(finalAngle))
			//		//{
			//		//    angle += rocketTiltRate;
			//		//    rotation += rocketTiltRate;
			//		//}
			//	}
			//	Rand.PopState();
			//}
		}

		private void BurnCells(Map map)
		{
			//if (burnRadius > 0 && VehicleMod.settings.main.burnRadiusOnRockets)
			//{
			//	foreach (IntVec3 intVec in CellsToBurn(drawPos.ToIntVec3(), map, burnRadius, null, null))
			//	{
			//		Rand.PushState();
			//		float fireSize = Rand.Range(0.65f, 0.95f);
			//		if (map.terrainGrid.TerrainAt(intVec).Flammable())
			//		{
			//			FireUtility.TryStartFireIn(intVec, map, fireSize);
			//		}
			//		foreach (Thing thing in map.thingGrid.ThingsAt(intVec))
			//		{
			//			if (thing == vehicle)
			//			{
			//				continue;
			//			}
			//			if (thing.FlammableNow)
			//			{
			//				if (thing is Pawn pawn)
			//				{
			//					TakeFireDamage(pawn, fireSize);
			//				}
			//				FireUtility.TryStartFireIn(intVec, map, fireSize);
			//			}
			//		}
			//		Rand.PopState();
			//	}
			//}
		}

		public static IEnumerable<IntVec3> CellsToBurn(IntVec3 center, Map map, float radius, IntVec3? needLOSToCell1 = null, IntVec3? needLOSToCell2 = null)
		{
			List<IntVec3> openCells = new List<IntVec3>();
			List<IntVec3> adjWallCells = new List<IntVec3>();
			int num = GenRadial.NumCellsInRadius(radius);
			for (int i = 0; i < num; i++)
			{
				IntVec3 intVec = center + GenRadial.RadialPattern[i];
				if (intVec.InBounds(map) && GenSight.LineOfSight(center, intVec, map, true, null, 0, 0))
				{
					if (needLOSToCell1 != null || needLOSToCell2 != null)
					{
						bool flag = needLOSToCell1 != null && GenSight.LineOfSight(needLOSToCell1.Value, intVec, map, false, null, 0, 0);
						bool flag2 = needLOSToCell2 != null && GenSight.LineOfSight(needLOSToCell2.Value, intVec, map, false, null, 0, 0);
						if (!flag && !flag2)
						{
							continue;
						}
					}
					openCells.Add(intVec);
				}
			}
			foreach (IntVec3 intVec2 in openCells)
			{
				if (intVec2.Walkable(map))
				{
					for (int k = 0; k < 4; k++)
					{
						IntVec3 intVec3 = intVec2 + GenAdj.CardinalDirections[k];
						if (intVec3.InHorDistOf(center, radius) && intVec3.InBounds(map) && !intVec3.Standable(map) && intVec3.GetEdifice(map) != null && !openCells.Contains(intVec3) && adjWallCells.Contains(intVec3))
						{
							adjWallCells.Add(intVec3);
						}
					}
				}
			}
			return openCells.Concat(adjWallCells);
		}

		private void TakeFireDamage(Pawn pawn, float fireSize)
		{
			int num = GenMath.RoundRandom(Mathf.Clamp(0.5f + 0.01f * fireSize, 0.25f, 1f) * 150f);
			if (num < 1)
			{
				num = 1;
			}
			BattleLogEntry_DamageTaken battleLogEntry_DamageTaken = new BattleLogEntry_DamageTaken(pawn, RulePackDefOf.DamageEvent_Fire, null);
			Find.BattleLog.Add(battleLogEntry_DamageTaken);
			DamageInfo dinfo = new DamageInfo(DamageDefOf.Flame, (float)num, 0f, -1f, vehicle, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null);
			dinfo.SetBodyRegion(BodyPartHeight.Undefined, BodyPartDepth.Outside);
			pawn.TakeDamage(dinfo).AssociateWithLog(battleLogEntry_DamageTaken);
			Apparel apparel;
			if (pawn.apparel != null && pawn.apparel.WornApparel.TryRandomElement(out apparel))
			{
				apparel.TakeDamage(new DamageInfo(DamageDefOf.Flame, (float)num, 0f, -1f, vehicle, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null));
				return;
			}
		}

		public override void ResolveProperties(LaunchProtocol reference)
		{
			base.ResolveProperties(reference);
			//RocketTakeoff rocketTakeoff = reference as RocketTakeoff;
			//finalAngle = rocketTakeoff.finalAngle;
			//rocketTiltRate = rocketTakeoff.rocketTiltRate;
			//thrusterSize = rocketTakeoff.thrusterSize;
			//dustSize = rocketTakeoff.dustSize;
			//burnRadius = rocketTakeoff.burnRadius;
		}
	}
}
