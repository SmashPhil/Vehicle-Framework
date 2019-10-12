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
using RimShips.Lords;
using RimShips.Jobs;
using UnityEngine;
using UnityEngine.AI;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Sound;

namespace RimShips.Lords
{
    public class LordToil_PrepareCaravan_GatherShip : LordToil
    {
        public LordToil_PrepareCaravan_GatherShip(List<Pawn> ships, IntVec3 meetingPoint)
        {
            this.meetingPoint = meetingPoint;
            this.ships = ships;
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
            foreach(Pawn pawn in this.lord.ownedPawns)
            {
                if(pawn.IsColonist)
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_GatherItems);
                }
                else if(pawn.RaceProps.Animal)
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf_Ships.PrepareCaravan_BoardShip);
                }
                else
                {
                    //CHANGE SO SHIPS DONT WANDER
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.PrepareCaravan_Wait);
                }
            }
        }

        public override void LordToilTick()
        {
            base.LordToilTick();
            if(Find.TickManager.TicksGame % 120 == 0)
            {
                bool flag = true;
                foreach(Pawn pawn in this.lord.ownedPawns)
                {
                    if(pawn.IsColonist && pawn.mindState.lastJobTag != JobTag.WaitingForOthersToFinishGatheringItems)
                    {
                        flag = false;
                        break;
                    }
                }
                if(flag)
                {
                    foreach(Pawn pawn in base.Map.mapPawns.AllPawnsSpawned)
                    {
                        if(pawn.CurJob != null && pawn.jobs.curDriver is JobDriver_PrepareCaravan_GatheringShip && pawn.CurJob.lord == this.lord)
                        {
                            flag = false;
                            break;
                        }
                    }
                }
                if(flag)
                {
                    this.lord.ReceiveMemo("AllItemsGathered");
                }
            }
        }

        private IntVec3 meetingPoint;

        private List<Pawn> ships;
    }
}
