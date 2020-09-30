using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Vehicles.Defs;
using Verse;
using Verse.AI;

namespace Vehicles.Jobs
{
    public class JobDriver_WorkOnUpgrade : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            int maxWorkers = Vehicle.GetCachedComp<CompVehicle>().TotalAllowedFor(JobDefOf_Vehicles.UpgradeVehicle);
            LocalTargetInfo target = Vehicle.GetCachedComp<CompVehicle>().SurroundingCells.First(c => pawn.Map.GetCachedMapComponent<VehicleReservationManager>().CanReserve<LocalTargetInfo, VehicleTargetReservation>(Vehicle, pawn, c));
            return target.IsValid && pawn.Map.GetCachedMapComponent<VehicleReservationManager>().Reserve<LocalTargetInfo, VehicleTargetReservation>(Vehicle, pawn, job, target, maxWorkers);
        }

        public VehiclePawn Vehicle
        {
            get
            {
                if(job.targetA.Thing is VehiclePawn vehicle)
                {
                    return vehicle;
                }
                Log.Error($"Cannot work on upgrade. TargetA: {TargetA.Thing.LabelShortCap} should be a vehicle");
                return null;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil work = new Toil();
            work.tickAction = delegate ()
            {
                Pawn actor = work.actor;
                if (!Vehicle.GetCachedComp<CompUpgradeTree>().CurrentlyUpgrading)
                {
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                    return;
                }
                actor.skills.Learn(SkillDefOf.Construction, 0.08f, false);
                float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
                if (statValue < 1)
                    statValue = 1;
                Vehicle.GetCachedComp<CompUpgradeTree>().NodeUnlocking.Ticks -= (int)statValue;
            };
            work.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            work.WithEffect(base.TargetThingA.def.repairEffect, TargetIndex.A);
            work.defaultCompleteMode = ToilCompleteMode.Never;
            work.activeSkill = (() => SkillDefOf.Construction);
            yield return work;
            yield break;
        }
    }
}
