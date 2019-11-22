using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
using RimShips.Defs;
using RimShips.Lords;
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
    public class JobGiver_CarryPawnToShip : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;
            Pawn pawn2 = this.FindDownedPawn(pawn);
            if (pawn2 is null)
                return null;
            Pawn ship = this.FindShipToDeposit(pawn, pawn2);
            ShipHandler handler = ship.GetComp<CompShips>().handlers.Find(x => x.role.handlingType == HandlingTypeFlags.None);
            return new Job(JobDefOf_Ships.CarryPawnToShip, pawn2, ship)
            {
                count = 1
            };
        }

        private Pawn FindDownedPawn(Pawn pawn)
        {
            Lord lord = pawn.GetLord();
            List<Pawn> downedPawns = ((LordJob_FormAndSendCaravanShip)lord.LordJob).downedPawns;
            foreach(Pawn comatose in downedPawns)
            {
                if(comatose.Downed && comatose != pawn && comatose.Spawned)
                {
                    if(pawn.CanReserveAndReach(comatose, PathEndMode.Touch, Danger.Deadly, 1, -1, null, false))
                    {
                        return comatose;
                    }
                }
            }
            return null;
        }

        private Pawn FindShipToDeposit(Pawn pawn, Pawn downedPawn)
        {
            List<Pawn> ships = pawn.GetLord().ownedPawns.Where(x => ShipHarmony.IsShip(x)).ToList();
            return ships.MaxBy(x => x.GetComp<CompShips>().Props.roles.Find(y => y.handlingType == HandlingTypeFlags.None).slots);
        }
    }
}
