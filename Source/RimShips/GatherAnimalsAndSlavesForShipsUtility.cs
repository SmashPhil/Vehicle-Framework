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

        public static void CheckArrived(Lord lord, List<Pawn> pawns, List<Pawn> ships, IntVec3 meetingPoint, string memo, Predicate<Pawn> shouldCheckIfArrived,
            Predicate<Pawn> extraValidator = null)
        {
            bool flag = true;
            foreach (Pawn p in pawns)
            {
                if(shouldCheckIfArrived(p))
                {
                    if (!p.Spawned || !p.Position.InHorDistOf(meetingPoint, 10f) || !p.CanReach(meetingPoint, PathEndMode.ClosestTouch, Danger.Deadly, false,
                        TraverseMode.ByPawn) || (extraValidator != null && !extraValidator(p)))
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
