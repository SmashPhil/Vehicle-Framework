using System;
using System.Collections.Generic;
using RimShips.AI;
using RimShips.Lords;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace RimShips
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
            foreach(Pawn p in pawnsToCheck)
            {
                if(shouldCheckIfArrived(p) && !waterPathing)
                {
                    if (!p.Spawned || !p.Position.InHorDistOf(meetingPoint, 15f) || !ReachabilityUtility.CanReach(p, meetingPoint, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn) 
                        || (extraValidator != null && !extraValidator(p)))
                    {
                        flag = false;
                        break;
                    }
                }
                else if(waterPathing)
                {
                    Pawn leadShip = ((LordJob_FormAndSendCaravanShip)lord.LordJob).LeadShip;
                    if (!p.Spawned || !p.Position.InHorDistOf(((LordJob_FormAndSendCaravanShip)lord.LordJob).LeadShip.Position, 5f) || !leadShip.Position.InHorDistOf(meetingPoint, leadShip.def.size.z > 5 ? (float)leadShip.def.size.z/2 : 3f) ||
                        !ShipReachabilityUtility.CanReachShip(p, meetingPoint, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn) || (extraValidator != null && !extraValidator(p)))
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
