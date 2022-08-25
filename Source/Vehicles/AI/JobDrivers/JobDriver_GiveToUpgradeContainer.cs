using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class JobDriver_GiveToUpgradeContainer : JobDriver_GiveToVehicle
	{
		public ThingDefCountClass ThingDef => new ThingDefCountClass(Item.def, Item.stackCount);

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return base.TryMakePreToilReservations(errorOnFailed) && pawn.Map.GetCachedMapComponent<VehicleReservationManager>().Reserve<ThingDefCountClass, VehicleNodeReservation>(Vehicle, pawn, job, ThingDef);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(TargetIndex.A);
			this.FailOnDestroyedOrNull(TargetIndex.B);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOn(() => !Vehicle.CompUpgradeTree.CurrentlyUpgrading 
				|| !Vehicle.CompUpgradeTree.NodeUnlocking.AvailableSpace(Item));
			yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false);
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOn(() => !Vehicle.CompUpgradeTree.CurrentlyUpgrading 
				|| !Vehicle.CompUpgradeTree.NodeUnlocking.AvailableSpace(Item));
			yield return GiveAsMuchToShipAsPossible();
			yield return Toils_Jump.JumpIf(FindNearestVehicle(), () => pawn.carryTracker.CarriedThing != null);
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
						ThingDefCountClass materialRequired = Vehicle.CompUpgradeTree.NodeUnlocking.MaterialsRequired().FirstOrDefault(x => x.thingDef == Item.def);
						
						if(ThingDef is null || ThingDef.count <= 0)
						{
							pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
						}
						else
						{
							int count = Mathf.Min(ThingDef.count, Item.stackCount); //Check back here
							pawn.carryTracker.innerContainer.TryTransferToContainer(Item, Vehicle.CompUpgradeTree.NodeUnlocking.itemContainer, count, true);
							pawn.Map.GetCachedMapComponent<VehicleReservationManager>().ReleaseAllClaimedBy(pawn);
							if(Vehicle.CompUpgradeTree.NodeUnlocking.StoredCostSatisfied)
							{
								pawn.Map.GetCachedMapComponent<VehicleReservationManager>().ClearReservedFor(Vehicle);
							}
						}
					}
				}
			};
		}
	}
}
