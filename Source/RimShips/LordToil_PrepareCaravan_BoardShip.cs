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
        public LordToil_PrepareCaravan_BoardShip(Pawn shipToBoard)
        {
            this.shipToBoard = shipToBoard;
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
                    p.mindState.duty = new PawnDuty(DutyDefOf_Ships.PrepareCaravan_BoardShip);
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

                    }
                }
            }
        }

        private Pawn shipToBoard;
    }
}
