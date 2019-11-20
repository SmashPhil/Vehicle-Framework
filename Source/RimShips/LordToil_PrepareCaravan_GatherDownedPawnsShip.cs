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
    public class LordToil_PrepareCaravan_GatherDownedPawnsShip : LordToil
    {
        public LordToil_PrepareCaravan_GatherDownedPawnsShip(IntVec3 meetingPoint, IntVec3 exitSpot)
        {
            this.meetingPoint = meetingPoint;
            this.exitSpot = exitSpot;
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
                if(p.IsColonist)
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf_Ships.PrepareCaravan_GatherDownedPawns, this.meetingPoint, this.exitSpot, -1f);
                }
                else
                {
                    p.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait, this.meetingPoint, -1f);
                }
            }
        }

        public override void LordToilTick()
        {
            if(Find.TickManager.TicksGame % 100 == 0)
            {
                List<Pawn> downedPawns = ((LordJob_FormAndSendCaravanShip)this.lord.LordJob).downedPawns;
                List<Pawn> ships = ((LordJob_FormAndSendCaravanShip)this.lord.LordJob).ships;
                List<Pawn> pawnsOnShips = new List<Pawn>();

                foreach(Pawn p in ships)
                {
                    pawnsOnShips.AddRange(p.GetComp<CompShips>().AllPawnsAboard);
                }

                Log.Message("-> " + pawnsOnShips.Count);
                Log.Message("2: " + downedPawns.Count);
                if(pawnsOnShips.Intersect(downedPawns).Count() == downedPawns.Count())
                { 
                    this.lord.ReceiveMemo("AllDownedPawnsGathered");
                }

            }
        }

        private IntVec3 meetingPoint;

        private IntVec3 exitSpot;
    }
}
