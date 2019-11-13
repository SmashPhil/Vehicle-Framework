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
            bool flag2;
            foreach (Pawn p in pawnsToCheck)
            {
                if(shouldCheckIfArrived(p))
                {
                    flag2 = waterPathing ? ShipReachabilityUtility.CanReachShip(p, meetingPoint, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn) :
                        ReachabilityUtility.CanReach(p, meetingPoint, PathEndMode.ClosestTouch, Danger.Deadly, false, TraverseMode.ByPawn);
                    if (!p.Spawned || !p.Position.InHorDistOf(meetingPoint, 10f) || !flag2 || (extraValidator != null && !extraValidator(p)))
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
