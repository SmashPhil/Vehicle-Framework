using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class JobDriver_LoadVehicle : JobDriver
{
	protected virtual string ListerTag => ReservationType.LoadVehicle;

	public virtual Thing Item
	{
		get { return job.GetTarget(TargetIndex.A).Thing; }
	}

	protected virtual VehiclePawn Vehicle
	{
		get { return job.GetTarget(TargetIndex.B).Thing as VehiclePawn; }
	}

	protected virtual bool FailJob()
	{
		return !MapComponentCache<VehicleReservationManager>.GetComponent(Map)
		 .VehicleListed(Vehicle, ListerTag);
	}

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		if (!pawn.Reserve(Item, job))
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
		this.FailOnForbidden(TargetIndex.A);
		this.FailOn(FailJob);
		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
		yield return Toils_Haul.StartCarryThing(TargetIndex.A);
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
		 .FailOnDespawnedNullOrForbidden(TargetIndex.B);
		yield return Toils_General.Wait(25).WithProgressBarToilDelay(TargetIndex.B);
		yield return GiveAsMuchToVehicleAsPossible();
	}

	protected virtual Toil FindNearestVehicle()
	{
		return new Toil
		{
			initAction = delegate
			{
				if (CaravanHelper.UsableVehicleWithTheMostFreeSpace(pawn) is { } vehicle)
				{
					job.SetTarget(TargetIndex.B, vehicle);
				}
				else
				{
					pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
				}
			}
		};
	}

	protected virtual Toil GiveAsMuchToVehicleAsPossible()
	{
		return new Toil
		{
			initAction = delegate
			{
				if (Item is null || Item.stackCount == 0)
				{
					pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
				}
				else
				{
					if (Item is Pawn hauledPawn)
					{
						// Try to add pawns to vehicle roles first, otherwise allow them to be stuffed in the inventory if allowed.
						if (Vehicle.TryAddPawn(hauledPawn) || !hauledPawn.CanBeTransferredToVehiclesCargo())
							return;
					}
					Vehicle.AddOrTransfer(Item, Item.stackCount);
				}
			}
		};
	}

	public static TransferableOneWay GetTransferable(List<TransferableOneWay> transferables,
		VehiclePawn vehicle, Thing thing)
	{
		foreach (TransferableOneWay transferable in transferables)
		{
			foreach (Thing transferableThing in transferable.things)
			{
				if (transferableThing == thing)
					return transferable;
			}
		}
		//Unable to find thing instance, match on def
		foreach (TransferableOneWay transferable in transferables)
		{
			foreach (Thing transferableThing in transferable.things)
			{
				if (transferableThing.def == thing.def)
					return transferable;
			}
		}
		return null;
	}
}