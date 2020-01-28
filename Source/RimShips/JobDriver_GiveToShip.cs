using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace RimShips.Jobs
{
    public class JobDriver_GiveToShip : JobDriver
    {
        public Thing Item
        {
            get
            {
                return job.GetTarget(TargetIndex.A).Thing;
            }
        }

        public Pawn Ship
        {
            get
            {
                Pawn p = job.GetTarget(TargetIndex.B).Thing as Pawn;
                return p;
            }
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(this.Item, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false);
            yield return this.FindNearestShip();
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.B);//.JumpIf(() => !MassUtility.WillBeOverEncumberedAfterPickingUp(Ship, Item, 1),
                //FindNearestShip()); //REDO LATER
            yield return GiveAsMuchToShipAsPossible();
            yield return Toils_Jump.JumpIf(FindNearestShip(), () => pawn.carryTracker.CarriedThing != null);
            yield break;
        }

        private Toil FindNearestShip()
        {
            return new Toil
            {
                initAction = delegate ()
                {
                    Pawn pawn = HelperMethods.UsableBoatWithTheMostFreeSpace(this.pawn);
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

        private Toil GiveAsMuchToShipAsPossible()
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
                        int count = Mathf.Min(MassUtility.CountToPickUpUntilOverEncumbered(Ship, Item), Item.stackCount);
                        pawn.carryTracker.innerContainer.TryTransferToContainer(Item, Ship.inventory.innerContainer, count, true);
                    }
                }
            };
        }
    }
}
