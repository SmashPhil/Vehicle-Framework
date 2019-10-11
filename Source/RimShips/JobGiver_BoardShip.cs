using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using RimShips.AI;
using RimShips.Defs;
using RimShips.Lords;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.Jobs
{
    public class  JobGiver_BoardShip : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Lord lord = pawn.GetLord();
            return pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) ? new Job(JobDefOf_Ships.Board, FindAvailableShip(pawn)) : null;
        }

        private Pawn FindAvailableShip(Pawn pawn)
        {
            return ((LordJob_FormAndSendCaravanShip)pawn.GetLord().LordJob).GetAvailableShip;
        }
        
        private Pawn targetShip;

        private LocomotionUrgency locomotionUrgency = LocomotionUrgency.Jog;

        private Danger maxDanger = Danger.Deadly;

        private int jobMaxDuration = 999999;
    }
}
