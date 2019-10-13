using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimShips.Defs;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;


namespace RimShips.Jobs
{
    public class JobGiver_PrepareCaravan_GatheringShip : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                return null;
            }
            Lord lord = pawn.GetLord();
            Thing thing = GatherItemsForShipCaravanUtility.FindThingToHaul(pawn, lord);
            if (thing is null)
            {
                return null;
            }
            return new Job(JobDefOf_Ships.PrepareCaravan_GatheringShip, thing)
            {
                lord = lord
            };
        }
    }
}
