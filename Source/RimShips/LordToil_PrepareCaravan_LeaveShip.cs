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
using RimShips.Build;
using RimShips.Defs;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.Lords
{
    public class LordToil_PrepareCaravan_LeaveShip : LordToil
    {
        public LordToil_PrepareCaravan_LeaveShip(IntVec3 exitSpot)
        {
            this.exitSpot = exitSpot;
        }

        public override bool AllowSatisfyLongNeeds => false;

        public override float? CustomWakeThreshold => new float?(0.5f);

        public override bool AllowRestingInBed => false;

        public override bool AllowSelfTend => false;

        public override void UpdateAllDuties()
        {
            foreach(Pawn p in this.lord.ownedPawns)
            {
                p.mindState.duty = new PawnDuty(DutyDefOf_Ships.TravelOrWaitOcean, this.exitSpot, -1f);
                p.mindState.duty.locomotion = LocomotionUrgency.Jog;
            }
        }

        public override void LordToilTick()
        {
            if(Find.TickManager.TicksGame % 100 == 0)
            {
                GatherAnimalsAndSlavesForShipsUtility.CheckArrived(this.lord, this.lord.ownedPawns.Where(x => ShipHarmony.IsShip(x)).ToList(), this.exitSpot, "ReadyToExitMap", (Pawn x) => true, true, null);
            }
        }

        private IntVec3 exitSpot;
    }
}
