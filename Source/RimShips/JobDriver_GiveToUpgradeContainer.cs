using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;

namespace Vehicles.Jobs
{
    public class JobDriver_GiveToUpgradeContainer : JobDriver_GiveToShip
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOn(() => !Ship.GetComp<CompUpgradeTree>().CurrentlyUpgrading 
                || !Ship.GetComp<CompUpgradeTree>().NodeUnlocking.AvailableSpace(Item));
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false);
            yield return FindNearestShip();
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOn(() => !Ship.GetComp<CompUpgradeTree>().CurrentlyUpgrading 
                || !Ship.GetComp<CompUpgradeTree>().NodeUnlocking.AvailableSpace(Item));
            yield return GiveAsMuchToShipAsPossible();
            yield return Toils_Jump.JumpIf(FindNearestShip(), () => pawn.carryTracker.CarriedThing != null);
            yield break;
        }

        protected override Toil FindNearestShip()
        {
            return new Toil();
        }

        protected override Toil GiveAsMuchToShipAsPossible()
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
                        ThingDefCountClass materialRequired = Ship.GetComp<CompUpgradeTree>().NodeUnlocking.MaterialsNeeded().Find(x => x.thingDef == Item.def);
                        if(materialRequired is null || materialRequired.count <= 0)
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                        }
                        else
                        {
                            int count = Mathf.Min(materialRequired.count, Item.stackCount); //Check back here
                            pawn.carryTracker.innerContainer.TryTransferToContainer(Item, Ship.GetComp<CompUpgradeTree>().NodeUnlocking.itemContainer, count, true);
                        }
                    }
                }
            };
        }
    }
}
