using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimShips.Defs;
using RimShips.AI;
using RimShips.Lords;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

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
                    if (!p.Spawned || !p.Position.InHorDistOf(meetingPoint, 10f) || !ReachabilityUtility.CanReach(p, meetingPoint, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn) 
                        || (extraValidator != null && !extraValidator(p)))
                    {
                        flag = false;
                        break;
                    }
                }
                else if(waterPathing)
                {
                    if(!p.Spawned || !p.Position.InHorDistOf(((LordJob_FormAndSendCaravanShip)lord.LordJob).LeadShip.Position, 5f) || !((LordJob_FormAndSendCaravanShip)lord.LordJob).LeadShip.Position.InHorDistOf(meetingPoint, 2f) ||
                        !ShipReachabilityUtility.CanReachShip(p, meetingPoint, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn) || (extraValidator != null && !extraValidator(p)))
                    {
                        Log.Message("Not passed");
                        Log.Message("1=> " + p.Position.InHorDistOf(((LordJob_FormAndSendCaravanShip)lord.LordJob).LeadShip.Position, 5f));
                        Log.Message("2=> " + ((LordJob_FormAndSendCaravanShip)lord.LordJob).LeadShip.Position.InHorDistOf(meetingPoint, 2f));
                        Log.Message("3=> " + ShipReachabilityUtility.CanReachShip(p, meetingPoint, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn));
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
