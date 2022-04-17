using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using SmashTools;

namespace Vehicles
{
	public static class ExitMapUtility
	{
		public static bool IsFollowingAnyone(Pawn p)
		{
			return p.mindState.duty.focus.HasThing;
		}

		public static void SetFollower(Pawn p, Pawn follower)
		{
			p.mindState.duty.focus = follower;
			p.mindState.duty.radius = 10f;
		}

		public static void CheckArrived(Lord lord, List<Pawn> pawnsToCheck, IntVec3 meetingPoint, string memo, Predicate<Pawn> shouldCheckIfArrived, Predicate<Pawn> extraValidator = null)
		{
			bool flag = true;
			VehiclePawn leadVehicle = ((LordJob_FormAndSendVehicles)lord.LordJob).LeadVehicle;
			foreach (Pawn pawn in pawnsToCheck)
			{
				if (shouldCheckIfArrived(pawn))
				{
					bool unspawned = !pawn.Spawned;

					bool pawnTooFar;
					bool cantReachExit;
					if (pawn is VehiclePawn vehicle)
					{
						pawnTooFar = !vehicle.Position.InHorDistOf(meetingPoint, 1);
						cantReachExit = !VehicleReachabilityUtility.CanReachVehicle(pawn as VehiclePawn, meetingPoint, PathEndMode.ClosestTouch, Danger.Deadly);
					}
					else
					{
						pawnTooFar = !pawn.Position.InHorDistOf(leadVehicle.Position, 5f);
						cantReachExit = !ReachabilityUtility.CanReach(pawn, leadVehicle.Position, PathEndMode.ClosestTouch, Danger.Deadly);
					}
					bool failedValidation = extraValidator != null && !extraValidator(pawn);
					if (unspawned || pawnTooFar || cantReachExit || failedValidation)
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				lord.ReceiveMemo(memo);
			}
		}
	}
}
