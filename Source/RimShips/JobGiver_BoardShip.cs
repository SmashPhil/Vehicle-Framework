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
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving)) return null;
            Pawn ship = null;
            if(pawn.GetLord().LordJob is LordJob_FormAndSendCaravanShip)
            {
                ship = ((LordJob_FormAndSendCaravanShip)pawn.GetLord().LordJob).GetShipAssigned(pawn);
                if(ship is null)
                {
                    ship = ((LordJob_FormAndSendCaravanShip)pawn.GetLord().LordJob).ships.First();
                }
            }
            
            return new Job(JobDefOf_Ships.Board, ship)
            {
                lord = pawn.GetLord()
            };
        }
    }
}
