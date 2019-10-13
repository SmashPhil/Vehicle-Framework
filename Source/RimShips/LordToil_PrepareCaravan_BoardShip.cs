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
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.Lords
{
    public class LordToil_PrepareCaravan_BoardShip : LordToil
    {
        public LordToil_PrepareCaravan_BoardShip(IntVec3 gatherPoint, List<Pawn> ships)
        {
            this.ships = ships;
            this.gatherPoint = gatherPoint;
        }

        public override float? CustomWakeThreshold
        {
            get
            {
                return new float?(0.5f);
            }
        }

        public override bool AllowRestingInBed
        {
            get
            {
                return false;
            }
        }

        public override void UpdateAllDuties()
        {
            foreach(Pawn p in this.lord.ownedPawns)
            {
                if(p.GetComp<CompShips>() is null)
                {
                    Log.Message("Board ship for - " + p.LabelShort);
                    p.mindState.duty = new PawnDuty(DutyDefOf_Ships.PrepareCaravan_BoardShip);
                    p.mindState.duty.locomotion = LocomotionUrgency.Jog;
                }
            }
        }

        public override void LordToilTick()
        {
            if(Find.TickManager.TicksGame % 100 == 0)
            {
                GatherAnimalsAndSlavesForShipsUtility.CheckArrived(this.lord, this.lord.ownedPawns, ships, this.gatherPoint, "ReadyToBoardShips", (Pawn x) => true, null);
            }
        }

        private List<Pawn> ships;

        private IntVec3 gatherPoint;
    }
}
