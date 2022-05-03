using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using HarmonyLib;
using SmashTools;

namespace Vehicles
{
	public class JobDriver_GiveToVehicle : JobDriver
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
			if (!pawn.Reserve(Item, job, 1, -1, null, errorOnFailed))
			{
				return false;
			}
			pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job);
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(TargetIndex.A);
			this.FailOnDestroyedOrNull(TargetIndex.B);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
			yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false);
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.B);
			yield return GiveAsMuchToShipAsPossible();
		}

		protected virtual Toil FindNearestVehicle()
		{
			return new Toil
			{
				initAction = delegate ()
				{
					Pawn pawn = CaravanHelper.UsableVehicleWithTheMostFreeSpace(this.pawn);
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
						pawn.carryTracker.innerContainer.TryTransferToContainer(Item, Vehicle.inventory.innerContainer, Item.stackCount, true);
						TransferableOneWay transferable = Vehicle.cargoToLoad.FirstOrDefault(t => t.AnyThing is {def: var def} && def == Item.def);
                        if (transferable is null)
                        {
							pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
                        }
						AccessTools.Field(typeof(TransferableOneWay), "countToTransfer").SetValue(transferable, transferable.CountToTransfer - job.count);
					}
				}
			};
		}
	}
}
