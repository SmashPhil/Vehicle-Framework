using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace Vehicles.Jobs
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
                ticksToNextRepair = InitialRepairTickCount;
                permanentRepair = false;
            };
            repair.tickAction = delegate ()
            {
                Pawn actor = repair.actor;
                actor.skills.Learn(SkillDefOf.Construction, 0.08f, false);
                float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
                ticksToNextRepair -= statValue;
                if (ticksToNextRepair <= 0f)
                {
                    if (!(TargetThingA as Pawn).health.hediffSet.hediffs.Any())
                    {
                        actor.records.Increment(RecordDefOf.ThingsRepaired);
                        actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                        return;
                    }
                    
                    ticksToNextRepair += pawn?.GetComp<CompVehicle>()?.Props.ticksBetweenRepair ?? InitialRepairTickCount;

                    Hediff repairPart = (TargetThingA as Pawn).health.hediffSet.hediffs.First();

                    if( ( (repairPart is Hediff_MissingPart) || repairPart.IsPermanent() ) && !permanentRepair)
                    {
                        ticksToNextRepair *= PermanentDamageMultiplier;
                        permanentRepair = true;
                    }
                    else if((repairPart is Hediff_MissingPart) && permanentRepair)
                    {
                        HealBodyBart(repairPart.Part, (TargetThingA as Pawn));
                        permanentRepair = false;
                    }
                    else if(repairPart.IsPermanent() && permanentRepair)
                    {
                        (TargetThingA as Pawn).health.RemoveHediff(repairPart);
                        permanentRepair = false;
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
