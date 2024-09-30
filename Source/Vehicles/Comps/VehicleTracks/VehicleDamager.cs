using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public static class VehicleDamager
	{
		private const float FleeAngleIncrement = 45f / 2; //45 degrees per Rot8 angle

		private static readonly int PawnNotifyCellCount = GenRadial.NumCellsInRadius(4.5f);

		private static readonly FloatRange[] FleeAngleRanges = new FloatRange[]
		{
			new FloatRange(360 - FleeAngleIncrement, FleeAngleIncrement),
			new FloatRange(Rot8.NorthEast.AsAngle - FleeAngleIncrement, Rot8.NorthEast.AsAngle + FleeAngleIncrement),
			new FloatRange(Rot8.East.AsAngle - FleeAngleIncrement, Rot8.East.AsAngle + FleeAngleIncrement),
			new FloatRange(Rot8.SouthEast.AsAngle - FleeAngleIncrement, Rot8.SouthEast.AsAngle + FleeAngleIncrement),
			new FloatRange(Rot8.South.AsAngle - FleeAngleIncrement, Rot8.South.AsAngle + FleeAngleIncrement),
			new FloatRange(Rot8.SouthWest.AsAngle - FleeAngleIncrement, Rot8.SouthWest.AsAngle + FleeAngleIncrement),
			new FloatRange(Rot8.West.AsAngle - FleeAngleIncrement, Rot8.West.AsAngle + FleeAngleIncrement),
			new FloatRange(Rot8.NorthWest.AsAngle - FleeAngleIncrement, Rot8.NorthWest.AsAngle + FleeAngleIncrement),
		};

		public static void NotifyNearbyPawnsOfDangerousPosition(Map map, IntVec3 cell)
		{
			for (int i = 0; i < PawnNotifyCellCount; i++)
			{
				IntVec3 c = cell + GenRadial.RadialPattern[i];
				if (c.InBounds(map))
				{
					List<Thing> thingList = c.GetThingList(map);
					for (int j = 0; j < thingList.Count; j++)
					{
						if (thingList[j] is Pawn pawn && !(pawn is VehiclePawn) && pawn.RaceProps.intelligence >= Intelligence.ToolUser && !pawn.Dead && !pawn.Downed)
						{
							if (GenSight.LineOfSight(cell, pawn.Position, map, true, null, 0, 0))
							{
								pawn.Notify_DangerousPosition(cell);
							}
						}
					}
				}
			}
		}

		private static void Notify_DangerousPosition(this Pawn pawn, IntVec3 cell)
		{
			if (!pawn.Spawned)
			{
				return;
			}
			if (pawn.RaceProps.intelligence < Intelligence.ToolUser)
			{
				return;
			}
			if (PawnUtility.PlayerForcedJobNowOrSoon(pawn))
			{
				return;
			}

			if (!RCellFinder.TryFindDirectFleeDestination(cell, 9f, pawn, out IntVec3 fleeCell))
			{
				return;
			}
			ForcePawnFlee(pawn, fleeCell);
		}

		public static float FriendlyFireChance(VehiclePawn vehicle, Pawn pawn)
		{
			if (pawn.Faction != Faction.OfPlayer) return 0;

			float multiplier = 1;
			if (pawn.Faction == vehicle.Faction)
			{
				multiplier = 0.5f;
			}
			return multiplier * VehicleMod.settings.main.friendlyFire switch
			{
				VehicleTracksFriendlyFire.None => 0,
				VehicleTracksFriendlyFire.Vanilla => Find.Storyteller.difficulty.friendlyFireChanceFactor,
				VehicleTracksFriendlyFire.Custom => VehicleMod.settings.main.friendlyFireChance,
				_ => throw new NotImplementedException(nameof(VehicleTracksFriendlyFire)),
			};
		}

		public static void Notify_DangerousVehiclePath(this Pawn pawn, VehiclePawn vehicle)
		{
			if (pawn is VehiclePawn)
			{
				return;
			}
			if (!vehicle.Spawned || !pawn.Spawned)
			{
				return;
			}
			if (pawn.Downed || pawn.Dead || pawn.InMentalState)
			{
				return;
			}
			if (FriendlyFireChance(vehicle, pawn) == 0)
			{
				return;
			}
			if (PawnUtility.PlayerForcedJobNowOrSoon(pawn))
			{
				return;
			}
			Rot8 oppositeVehicle = vehicle.FullRotation.Opposite;
			Rot8 oppositeCW = oppositeVehicle.Rotated(RotationDirection.Clockwise);
			Rot8 oppositeCCW = oppositeVehicle.Rotated(RotationDirection.Counterclockwise);
			if (!TryFindDirectFleeDestination(vehicle.Position, vehicle.VehicleDef.Size.x * 5, pawn, out IntVec3 cell, oppositeVehicle, oppositeCW, oppositeCCW))
			{
				return;
			}
			ForcePawnFlee(pawn, cell);
		}

		private static void ForcePawnFlee(Pawn pawn, IntVec3 cell)
		{
			pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
			Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
			job.locomotionUrgency = LocomotionUrgency.Sprint;
			if (pawn.jobs.TryTakeOrderedJob(job))
			{
				MoteMaker.MakeColonistActionOverlay(pawn, ThingDefOf.Mote_ColonistFleeing);
			}
		}

		private static bool TryFindDirectFleeDestination(IntVec3 root, float dist, Pawn pawn, out IntVec3 result, params Rot8[] excludeDirections)
		{
			List<Rot8> directions = new List<Rot8>() { Rot8.North, Rot8.NorthEast, Rot8.East, Rot8.SouthEast, Rot8.South, Rot8.SouthWest, Rot8.West, Rot8.NorthWest };
			if (!excludeDirections.NullOrEmpty())
			{
				for (int i = 0; i < excludeDirections.Length; i++)
				{
					Rot8 rot = excludeDirections[i];
					directions.Remove(rot);
				}
			}
			Rand.PushState();
			try
			{
				for (int i = 0; i < 30; i++)
				{
					Rot8 rot = directions.RandomElementWithFallback(fallback: pawn.Rotation.Opposite);
					if (ImmediatelyWalkable(root, FleeAngleRanges[rot.AsIntClockwise], dist, pawn, out result))
					{
						return true;
					}
				}
			}
			finally
			{
				Rand.PopState();
			}

			//Failsafe check
			Region region = RegionAndRoomQuery.GetRegion(pawn, RegionType.Set_Passable);
			for (int j = 0; j < 30; j++)
			{
				IntVec3 randomCell = CellFinder.RandomRegionNear(region, 15, TraverseParms.For(pawn)).RandomCell;
				if (randomCell.Walkable(pawn.Map) && (root - randomCell).LengthHorizontalSquared > dist * dist)
				{
					using PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, randomCell, pawn, PathEndMode.OnCell);
					if (PawnPathUtility.TryFindCellAtIndex(pawnPath, (int)dist + 3, out result))
					{
						return true;
					}
				}
			}
			result = pawn.Position;
			return false;
		}

		private static bool ImmediatelyWalkable(IntVec3 root, FloatRange angleRange, float distance, Pawn pawn, out IntVec3 result)
		{
			float angle = angleRange.RandomInRange;
			IntVec3 offset = IntVec3.FromVector3(Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * distance);
			result = root + offset;
			if (result.Walkable(pawn.Map) && result.DistanceToSquared(pawn.Position) < result.DistanceToSquared(root) && GenSight.LineOfSight(root, result, pawn.Map, true))
			{
				if (VehicleMod.settings.debug.debugDrawFleePoint)
				{
					pawn.Map.debugDrawer.FlashCell(result, 0.5f);
					pawn.Map.debugDrawer.FlashLine(pawn.Position, result, color: SimpleColor.Green);
				}
				return true;
			}
			if (VehicleMod.settings.debug.debugDrawFleePoint && result.InBounds(pawn.Map))
			{
				pawn.Map.debugDrawer.FlashCell(result, 0);
				pawn.Map.debugDrawer.FlashLine(pawn.Position, result, color: SimpleColor.Red);
			}
			return false;
		}
	}
}
