using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicles
{
	public static class GenVehicleTracks
	{
		private static readonly int PawnNotifyCellCount = GenRadial.NumCellsInRadius(4.5f);

		public static void NotifyNearbyPawnsPathOfVehicleTrack(VehiclePawn vehicle)
		{
			Room room = RegionAndRoomQuery.GetRoom(vehicle, RegionType.Set_Passable);
			for (int i = 0; i < PawnNotifyCellCount; i++)
			{
				IntVec3 c = vehicle.Position + GenRadial.RadialPattern[i];
				if (c.InBounds(vehicle.Map))
				{
					List<Thing> thingList = c.GetThingList(vehicle.Map);
					for (int j = 0; j < thingList.Count; j++)
					{
						Pawn pawn = thingList[j] as Pawn;
						if (pawn != null && pawn.RaceProps.intelligence >= Intelligence.ToolUser)
						{
							Room room2 = RegionAndRoomQuery.GetRoom(pawn, RegionType.Set_Passable);
							if (room2 == null || room2.CellCount == 1 || (room2 == room && GenSight.LineOfSight(vehicle.Position, pawn.Position, vehicle.Map, true, null, 0, 0)))
							{
								pawn.Notify_DangerousVehiclePath(vehicle);
							}
						}
					}
				}
			}
		}

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

		private static void Notify_DangerousVehiclePath(this Pawn pawn, VehiclePawn vehicle)
		{
			if (pawn is VehiclePawn)
			{
				return;
			}
			if (!vehicle.Spawned || !pawn.Spawned)
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

			if (!TryFindDirectFleeDestination(vehicle.Position, 9f, vehicle.Rotation, pawn, out IntVec3 cell))
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

		private static bool TryFindDirectFleeDestination(IntVec3 root, float dist, Rot4 dirExcluded, Pawn pawn, out IntVec3 result)
		{
			for (int i = 0; i < 30; i++)
			{
				result = root + IntVec3.FromVector3(Vector3Utility.HorizontalVectorFromAngle(Rand.Range(0, 360)) * dist);
				if (result.Walkable(pawn.Map) && result.DistanceToSquared(pawn.Position) < result.DistanceToSquared(root) && GenSight.LineOfSight(root, result, pawn.Map, true, null, 0, 0))
				{
					return true;
				}
			}
			Region region = RegionAndRoomQuery.GetRegion(pawn, RegionType.Set_Passable);
			for (int j = 0; j < 30; j++)
			{
				IntVec3 randomCell = CellFinder.RandomRegionNear(region, 15, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), null, null, RegionType.Set_Passable).RandomCell;
				if (randomCell.Walkable(pawn.Map) && (float)(root - randomCell).LengthHorizontalSquared > dist * dist)
				{
					using (PawnPath pawnPath = pawn.Map.pathFinder.FindPath(pawn.Position, randomCell, pawn, PathEndMode.OnCell))
					{
						if (PawnPathUtility.TryFindCellAtIndex(pawnPath, (int)dist + 3, out result))
						{
							return true;
						}
					}
				}
			}
			result = pawn.Position;
			return false;
		}
	}
}
