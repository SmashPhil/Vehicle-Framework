using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using SmashTools;
using Vehicles.AI;
using Vehicles.Lords;

namespace Vehicles
{
	public static class GatherAnimalsAndSlavesForShipsUtility
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

		public static void CheckArrived(Lord lord, List<Pawn> pawnsToCheck, IntVec3 meetingPoint, string memo, Predicate<Pawn> shouldCheckIfArrived, bool waterPathing,
			Predicate<Pawn> extraValidator = null)
		{
			bool flag = true;
			foreach (Pawn p in pawnsToCheck)
			{
				VehiclePawn leadShip = ((LordJob_FormAndSendVehicles)lord.LordJob).LeadVehicle;
				if (waterPathing)
				{
					if (!p.Spawned || !p.Position.InHorDistOf(((LordJob_FormAndSendVehicles)lord.LordJob).LeadVehicle.Position, 5f) || !(leadShip.Position.InHorDistOf(meetingPoint, leadShip.def.size.z > 5 ? (float)leadShip.def.size.z/2 : 3f) || leadShip.Position.WithinDistanceToEdge(leadShip.def.size.z, leadShip.Map)) ||
					!VehicleReachabilityUtility.CanReachVehicle(p as VehiclePawn, meetingPoint, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn) || (extraValidator != null && !extraValidator(p)))
					{
						flag = false;
						break;
					}
				}
				else
				{
					if (!p.Spawned || !p.Position.InHorDistOf(((LordJob_FormAndSendVehicles)lord.LordJob).LeadVehicle.Position, 5f) || !(leadShip.Position.InHorDistOf(meetingPoint, leadShip.def.size.z > 5 ? (float)leadShip.def.size.z/2 : 3f) || leadShip.Position.WithinDistanceToEdge(leadShip.def.size.z, leadShip.Map)) ||
					!ReachabilityUtility.CanReach(p, meetingPoint, PathEndMode.ClosestTouch, Danger.Deadly, false) || (extraValidator != null && !extraValidator(p)))
					{
						flag = false;
						break;
					}
				}
				
			}
			if(flag)
			{
				lord.ReceiveMemo(memo);
			}
		}
	}
}
