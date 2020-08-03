using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using Vehicles.Defs;

namespace Vehicles.Jobs
{
    public class JobDriver_GiveToUpgradeContainer : JobDriver_GiveToShip
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            int maxWorkers = Vehicle.GetCachedComp<CompVehicle>().TotalAllowedFor(JobDefOf_Vehicles.LoadUpgradeMaterials);
            return base.TryMakePreToilReservations(errorOnFailed) && pawn.Map.GetComponent<VehicleReservationManager>().Reserve<ThingDefCountClass, VehicleNodeReservation>(Vehicle, pawn, job, ThingDef, maxWorkers);
        }

        public ThingDefCountClass ThingDef => new ThingDefCountClass(Item.def, Item.stackCount);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOn(() => !Vehicle.GetCachedComp<CompUpgradeTree>().CurrentlyUpgrading 
                || !Vehicle.GetCachedComp<CompUpgradeTree>().NodeUnlocking.AvailableSpace(Item));
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOn(() => !Vehicle.GetCachedComp<CompUpgradeTree>().CurrentlyUpgrading 
                || !Vehicle.GetCachedComp<CompUpgradeTree>().NodeUnlocking.AvailableSpace(Item));
            yield return GiveAsMuchToShipAsPossible();
            yield return Toils_Jump.JumpIf(FindNearestShip(), () => pawn.carryTracker.CarriedThing != null);
            yield break;
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
                        ThingDefCountClass materialRequired = Vehicle.GetCachedComp<CompUpgradeTree>().NodeUnlocking.MaterialsRequired().FirstOrDefault(x => x.thingDef == Item.def);
                        
                        if(ThingDef is null || ThingDef.count <= 0)
                        {
                            pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                        }
                        else
                        {
                            int count = Mathf.Min(ThingDef.count, Item.stackCount); //Check back here
                            pawn.carryTracker.innerContainer.TryTransferToContainer(Item, Vehicle.GetCachedComp<CompUpgradeTree>().NodeUnlocking.itemContainer, count, true);
                            pawn.Map.GetComponent<VehicleReservationManager>().ReleaseAllClaimedBy(pawn);
                            if(Vehicle.GetCachedComp<CompUpgradeTree>().NodeUnlocking.StoredCostSatisfied)
                            {
                                pawn.Map.GetComponent<VehicleReservationManager>().ClearReservedFor(Vehicle);
                            }
                        }
                    }
                }
            };
        }
    }
}
