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
    public class JobGiver_SendSlavesToShip : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return null;
            Pawn pawn2 = this.FindPrisoner(pawn);
            if (pawn2 is null)
                return null;
            Pawn ship = this.FindShipToDeposit(pawn, pawn2);
            ShipHandler handler = ship.GetComp<CompShips>().handlers.Find(x => x.role.handlingType == HandlingTypeFlags.None);
            return new Job(JobDefOf.PrepareCaravan_GatherPawns, pawn2)
            {
                count = 1
            };
        }

        private Pawn FindPrisoner(Pawn pawn)
        {
            Lord lord = pawn.GetLord();
            List<Pawn> prisoners = ((LordJob_FormAndSendCaravanShip)lord.LordJob).prisoners;
            foreach (Pawn slave in prisoners)
            {
                if(slave != pawn && slave.Spawned)
                {
                    if (pawn.CanReserveAndReach(slave, PathEndMode.Touch, Danger.Deadly, 1, -1, null, false))
                    {
                        return slave;
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
