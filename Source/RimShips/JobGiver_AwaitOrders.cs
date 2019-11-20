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

namespace RimShips.Jobs
{
    public class JobGiver_AwaitOrders : ThinkNode_JobGiver
    {
        public override ThinkNode DeepCopy(bool resolve = true)
        {
            JobGiver_AwaitOrders jobGiver_AwaitOrders = (JobGiver_AwaitOrders)base.DeepCopy(resolve);
            jobGiver_AwaitOrders.ticks = this.ticks;
            return jobGiver_AwaitOrders;
        }
        protected override Job TryGiveJob(Pawn pawn)
        {
            if(pawn.pather.Moving)
                return null;
            return new Job(JobDefOf_Ships.IdleShip);
        }

        public int ticks = 250;
    }
}