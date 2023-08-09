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
	public static class GenVehicleDamager
	{
		private static readonly int PawnNotifyCellCount = GenRadial.NumCellsInRadius(4.5f);

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
						if (thingList[j] is Pawn pawn && !(pawn is VehiclePawn) && pawn.RaceProps.intelligence >= Intelligence.ToolUser)
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
			pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
			Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
			job.locomotionUrgency = LocomotionUrgency.Sprint;
			pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
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

			if (!RCellFinder.TryFindDirectFleeDestination(cell, 9f, pawn, out IntVec3 cell2))
			{
				return;
			}
			pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
			Job job = JobMaker.MakeJob(JobDefOf.Goto, cell2);
			job.locomotionUrgency = LocomotionUrgency.Sprint;
			pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
		}

		private static bool TryFindDirectFleeDestination(IntVec3 root, float dist, Pawn pawn, out IntVec3 result, params Rot8[] excludeDirections)
		{
			float increment = 45f / 2; //45 degrees per Rot8 angle

			bool[] allowedDirections = new bool[8];
			allowedDirections.Populate(true);
			if (!excludeDirections.NullOrEmpty())
			{
				for (int i = 0; i < excludeDirections.Length; i++)
				{
					Rot8 rot = excludeDirections[i];
					allowedDirections[rot.AsInt] = false;
				}
			}
			Log.Message($"Finding flee destination with excluded directions: {string.Join(",", excludeDirections.Select(rot => rot.ToString()))}");
			for (int i = 0; i < 4 + (4 * excludeDirections.Length); i++) //8 tries x4 iterations for 32 attempts (accounts for excluded direction count to ensure same number of attempts
			{
				FloatRange angleNorth = new FloatRange(360 - increment, increment);
				if (allowedDirections[Rot8.NorthInt] && ImmediatelyWalkable(root, angleNorth, dist, pawn, out result))
				{
					return true;
				}
				FloatRange angleNorthEast = new FloatRange(angleNorth.max, angleNorth.max + 45);
				if (allowedDirections[Rot8.NorthEastInt] && ImmediatelyWalkable(root, angleNorthEast, dist, pawn, out result))
				{
					return true;
				}

				FloatRange angleEast = new FloatRange(angleNorthEast.max, angleNorthEast.max + 45);
				if (allowedDirections[Rot8.EastInt] && ImmediatelyWalkable(root, angleEast, dist, pawn, out result))
				{
					return true;
				}

				FloatRange angleSouthEast = new FloatRange(angleEast.max, angleEast.max + 45);
				if (allowedDirections[Rot8.SouthEastInt] && ImmediatelyWalkable(root, angleSouthEast, dist, pawn, out result))
				{
					return true;
				}

				FloatRange angleSouth = new FloatRange(angleSouthEast.max, angleSouthEast.max + 45);
				if (allowedDirections[Rot8.SouthInt] && ImmediatelyWalkable(root, angleSouth, dist, pawn, out result))
				{
					return true;
				}

				FloatRange angleSouthWest = new FloatRange(angleSouth.max, angleSouth.max + 45);
				if (allowedDirections[Rot8.SouthWestInt] && ImmediatelyWalkable(root, angleSouthWest, dist, pawn, out result))
				{
					return true;
				}

				FloatRange angleWest = new FloatRange(angleSouthWest.max, angleSouthWest.max + 45);
				if (allowedDirections[Rot8.WestInt] && ImmediatelyWalkable(root, angleWest, dist, pawn, out result))
				{
					return true;
				}

				FloatRange angleNorthWest = new FloatRange(angleWest.max, angleWest.max + 45);
				if (allowedDirections[Rot8.NorthWestInt] && ImmediatelyWalkable(root, angleNorthWest, dist, pawn, out result))
				{
					return true;
				}
			}
			Log.Message("Failsafe check");
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

		private static bool ImmediatelyWalkable(IntVec3 root, FloatRange angleRange, float dist, Pawn pawn, out IntVec3 result)
		{
			float radians = angleRange.RandomInRange * Mathf.Deg2Rad;
			int x = Mathf.RoundToInt(dist * Mathf.Cos(radians));
			int z = Mathf.RoundToInt(dist * Mathf.Sin(radians));
			result = root + new IntVec3(x, 0, z);
			if (result.Walkable(pawn.Map) && result.DistanceToSquared(pawn.Position) < result.DistanceToSquared(root) && GenSight.LineOfSight(root, result, pawn.Map, true))
			{
				pawn.Map.debugDrawer.FlashCell(result, 0.5f);
				pawn.Map.debugDrawer.FlashLine(pawn.Position, result, color: SimpleColor.Green);
				return true;
			}
			if (result.InBounds(pawn.Map))
			{
				pawn.Map.debugDrawer.FlashCell(result, 0);
				pawn.Map.debugDrawer.FlashLine(pawn.Position, result, color: SimpleColor.Red);
			}
			return false;
		}
	}
}
