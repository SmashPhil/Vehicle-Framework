using System;
using System.Collections.Generic;
using RimWorld;
using SmashTools;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Vehicles;

public static class VehicleDamager
{
	private const float FleeAngleIncrement = 45f / 2; //45 degrees per Rot8 angle

	private static readonly int PawnNotifyCellCount = GenRadial.NumCellsInRadius(4.5f);

	private static readonly FloatRange[] FleeAngleRanges =
	[
		new(360 - FleeAngleIncrement, FleeAngleIncrement),
		new(Rot8.NorthEast.AsAngle - FleeAngleIncrement,
			Rot8.NorthEast.AsAngle + FleeAngleIncrement),
		new(Rot8.East.AsAngle - FleeAngleIncrement,
			Rot8.East.AsAngle + FleeAngleIncrement),
		new(Rot8.SouthEast.AsAngle - FleeAngleIncrement,
			Rot8.SouthEast.AsAngle + FleeAngleIncrement),
		new(Rot8.South.AsAngle - FleeAngleIncrement,
			Rot8.South.AsAngle + FleeAngleIncrement),
		new(Rot8.SouthWest.AsAngle - FleeAngleIncrement,
			Rot8.SouthWest.AsAngle + FleeAngleIncrement),
		new(Rot8.West.AsAngle - FleeAngleIncrement,
			Rot8.West.AsAngle + FleeAngleIncrement),
		new(Rot8.NorthWest.AsAngle - FleeAngleIncrement,
			Rot8.NorthWest.AsAngle + FleeAngleIncrement),
	];

	public static void NotifyNearbyPawnsOfDangerousPosition(Map map, VehiclePawn vehicle,
		IntVec3 cell)
	{
		for (int i = 0; i < PawnNotifyCellCount; i++)
		{
			IntVec3 c = cell + GenRadial.RadialPattern[i];
			if (c.InBounds(map))
			{
				foreach (Thing thing in c.GetThingList(map))
				{
					if (thing is Pawn pawn && GenSight.LineOfSight(cell, pawn.Position, map, true))
					{
						Notify_DangerousPosition(pawn, vehicle, cell);
					}
				}
			}
		}
	}

	private static void Notify_DangerousPosition(Pawn pawn, VehiclePawn vehicle, IntVec3 cell)
	{
		if (!ShouldFleeDangerZone(pawn, vehicle, out float fleeDist))
			return;
		// Additional multiplier to more forcefully nudge them out of the way for AerialVehicle related damagers.
		if (!RCellFinder.TryFindDirectFleeDestination(cell, fleeDist * 1.9f, pawn,
			out IntVec3 fleeCell))
		{
			return;
		}
		ForcePawnFlee(pawn, fleeCell);
	}

	public static float FriendlyFireChance(VehiclePawn vehicle, Pawn pawn)
	{
		float multiplier = 1;
		if (pawn.Faction != vehicle.Faction)
		{
			// Running over other factions due to bad pathing is even worse, apply additional hidden nerf
			// to avoid accidental incidents that would ruin faction relations.
			multiplier = 0.5f;
		}
		return multiplier * VehicleMod.settings.main.friendlyFire switch
		{
			VehicleTracksFriendlyFire.None    => 0,
			VehicleTracksFriendlyFire.Vanilla => Find.Storyteller.difficulty.friendlyFireChanceFactor,
			VehicleTracksFriendlyFire.Custom  => VehicleMod.settings.main.friendlyFireChance,
			_                                 => throw new NotImplementedException(nameof(VehicleTracksFriendlyFire)),
		};
	}

	public static void Notify_DangerousVehiclePath(this Pawn pawn, VehiclePawn vehicle)
	{
		if (!ShouldFleeDangerZone(pawn, vehicle, out float fleeDist))
			return;

		Rot8 oppositeVehicle = vehicle.FullRotation.Opposite;
		Rot8 oppositeCw = oppositeVehicle.Rotated(RotationDirection.Clockwise);
		Rot8 oppositeCcw = oppositeVehicle.Rotated(RotationDirection.Counterclockwise);
		if (!TryFindDirectFleeDestination(vehicle.Position, fleeDist, pawn, out IntVec3 cell,
			oppositeVehicle, oppositeCw, oppositeCcw))
			return;

		ForcePawnFlee(pawn, cell);
	}

	private static bool ShouldFleeDangerZone(Pawn pawn, VehiclePawn vehicle, out float distance)
	{
		const float DefaultDistance = 5;

		distance = DefaultDistance;

		if (pawn is VehiclePawn)
			return false;
		if (!vehicle.Spawned || !pawn.Spawned)
			return false;
		if (pawn.Downed || pawn.Dead || pawn.InMentalState)
			return false;
		if (FriendlyFireChance(vehicle, pawn) == 0)
			return false;
		if (PawnUtility.PlayerForcedJobNowOrSoon(pawn))
			return false;

		// Disallowed races
		if (pawn.RaceProps.intelligence < Intelligence.ToolUser)
			return false;
		if (pawn.RaceProps.IsMechanoid || pawn.RaceProps.Insect || pawn.IsMutant)
			return false;

		distance *= vehicle.VehicleDef.Size.x;
		PawnFleeSettingsDefModExtension pawnFleeSettings =
			pawn.kindDef.GetModExtension<PawnFleeSettingsDefModExtension>();
		// If a modded race is to skip fleeing entirely, here would be the hook for it.
		if (pawnFleeSettings is not null)
		{
			if (!pawnFleeSettings.shouldFlee)
				return false;
			if (pawnFleeSettings.fleeDistance > 0)
				distance = pawnFleeSettings.fleeDistance;
		}
		return true;
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

	private static bool TryFindDirectFleeDestination(IntVec3 root, float dist, Pawn pawn,
		out IntVec3 result, params Rot8[] excludeDirections)
	{
		List<Rot8> directions =
		[
			Rot8.North, Rot8.NorthEast, Rot8.East, Rot8.SouthEast, Rot8.South, Rot8.SouthWest,
			Rot8.West, Rot8.NorthWest
		];
		if (!excludeDirections.NullOrEmpty())
		{
			foreach (Rot8 rot in excludeDirections)
			{
				directions.Remove(rot);
			}
		}
		Rand.PushState();
		try
		{
			for (int i = 0; i < 30; i++)
			{
				Rot8 rot = directions.RandomElementWithFallback(fallback: pawn.Rotation.Opposite);
				if (ImmediatelyWalkable(root, FleeAngleRanges[rot.AsIntClockwise], dist, pawn,
					out result))
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
		Region region = pawn.GetRegion();
		for (int j = 0; j < 30; j++)
		{
			IntVec3 randomCell = CellFinder.RandomRegionNear(region, 15, TraverseParms.For(pawn))
			 .RandomCell;
			if (randomCell.Walkable(pawn.Map) &&
				(root - randomCell).LengthHorizontalSquared > dist * dist)
			{
				using PawnPath pawnPath =
					pawn.Map.pathFinder.FindPathNow(pawn.Position, randomCell, pawn,
						peMode: PathEndMode.OnCell);
				if (PawnPathUtility.TryFindCellAtIndex(pawnPath, (int)dist + 3, out result))
				{
					return true;
				}
			}
		}
		result = pawn.Position;
		return false;
	}

	private static bool ImmediatelyWalkable(IntVec3 root, FloatRange angleRange, float distance,
		Pawn pawn, out IntVec3 result)
	{
		float angle = angleRange.RandomInRange;
		IntVec3 offset =
			IntVec3.FromVector3(Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * distance);
		result = root + offset;
		if (result.Walkable(pawn.Map) &&
			result.DistanceToSquared(pawn.Position) < result.DistanceToSquared(root) &&
			GenSight.LineOfSight(root, result, pawn.Map, true))
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
			pawn.Map.debugDrawer.FlashCell(result);
			pawn.Map.debugDrawer.FlashLine(pawn.Position, result, color: SimpleColor.Red);
		}
		return false;
	}
}