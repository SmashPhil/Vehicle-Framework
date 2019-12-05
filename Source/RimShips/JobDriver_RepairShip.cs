using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Harmony;
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
    public class JobDriver_RepairShip : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(this.job.targetA, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil repair = new Toil();
            repair.initAction = delegate ()
            {
                this.ticksToNextRepair = InitialRepairTickCount;
                this.permanentRepair = false;
            };
            repair.tickAction = delegate ()
            {
                Pawn actor = repair.actor;
                actor.skills.Learn(SkillDefOf.Construction, 0.08f, false);
                float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
                this.ticksToNextRepair -= statValue;
                if (this.ticksToNextRepair <= 0f)
                {
                    if (!(this.TargetThingA as Pawn).health.hediffSet.hediffs.Any())
                    {
                        actor.records.Increment(RecordDefOf.ThingsRepaired);
                        actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                        return;
                    }

                    this.ticksToNextRepair += this.pawn?.GetComp<CompShips>()?.Props.ticksBetweenRepair ?? InitialRepairTickCount;
                    Hediff repairPart = (this.TargetThingA as Pawn).health.hediffSet.hediffs.First();

                    if( ( (repairPart is Hediff_MissingPart) || repairPart.IsPermanent() ) && !this.permanentRepair)
                    {
                        this.ticksToNextRepair *= PermanentDamageMultiplier;
                        this.permanentRepair = true;
                    }
                    else if((repairPart is Hediff_MissingPart) && permanentRepair)
                    {
                        this.HealBodyBart(repairPart.Part, (this.TargetThingA as Pawn));
                        this.permanentRepair = false;
                    }
                    else if(repairPart.IsPermanent() && permanentRepair)
                    {
                        (this.TargetThingA as Pawn).health.RemoveHediff(repairPart);
                        this.permanentRepair = false;
                    }
                    else
                    {
                        repairPart.Heal(repairHealAmount);
                    }
                }
            };
            repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            repair.WithEffect(base.TargetThingA.def.repairEffect, TargetIndex.A);
            repair.defaultCompleteMode = ToilCompleteMode.Never;
            repair.activeSkill = (() => SkillDefOf.Construction);
            yield return repair;
            //Ship is repaired message?
            yield break;
        }

        protected void HealBodyBart(BodyPartRecord part, Pawn pawn)
        {
            pawn.health.RestorePart(part);
        }

        protected float ticksToNextRepair;

        private const float InitialRepairTickCount = 100f;

        private const float PermanentDamageMultiplier = 10f;

        private const float repairHealAmount = 0.1f;

        private bool permanentRepair;
    }
}
