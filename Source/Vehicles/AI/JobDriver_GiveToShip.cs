using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicles.Jobs
{
    public class JobDriver_GiveToShip : JobDriver
    {
        public virtual Thing Item
        {
            get
            {
                return job.GetTarget(TargetIndex.A).Thing;
            }
        }

        public virtual VehiclePawn Vehicle
        {
            get
            {
                return job.GetTarget(TargetIndex.B).Thing as VehiclePawn;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Item, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false);
            //yield return FindNearestShip();
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.B);
            yield return GiveAsMuchToShipAsPossible();
            //yield return Toils_Jump.JumpIf(FindNearestShip(), () => pawn.carryTracker.CarriedThing != null);
            yield break;
        }

        protected virtual Toil FindNearestShip()
        {
            return new Toil
            {
                initAction = delegate ()
                {
                    Pawn pawn = HelperMethods.UsableVehicleWithTheMostFreeSpace(this.pawn);
                    if (pawn is null)
                    {
                        this.pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                    }
                    else
                    {
                        this.job.SetTarget(TargetIndex.B, pawn);
                    }
                }
            };
        }

        protected virtual Toil GiveAsMuchToShipAsPossible()
        {
            return new Toil
            {
                initAction = delegate ()
                {
                    if(Item is null)
                    {
                        pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                    }
                    else
                    {
                        int count = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(Vehicle, Item), Item.stackCount);
                        pawn.carryTracker.innerContainer.TryTransferToContainer(Item, Vehicle.inventory.innerContainer, count, true);
                    }
                }
            };
        }
    }
}
