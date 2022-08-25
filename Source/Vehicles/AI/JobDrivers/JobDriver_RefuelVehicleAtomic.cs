using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace Vehicles
{
	public class JobDriver_RefuelVehicleAtomic : JobDriver
	{
		private const int RefuelingDuration = 240;

		protected VehiclePawn Vehicle
		{
			get
			{
				return job.GetTarget(TargetIndex.A).Thing as VehiclePawn;
			}
		}

		protected Thing Fuel
		{
			get
			{
				return job.GetTarget(TargetIndex.B).Thing;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Vehicle, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Fuel, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			AddEndCondition(delegate
			{
				if (!Vehicle.CompFueledTravel.FullTank)
				{
					return JobCondition.Ongoing;
				}
				return JobCondition.Succeeded;
			});
			yield return Toils_General.DoAtomic(delegate
			{
				job.count = Vehicle.CompFueledTravel.FuelCountToFull;
			});
			Toil reserveFuel = Toils_Reserve.Reserve(TargetIndex.B);
			yield return reserveFuel;
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
			yield return Toils_Haul.StartCarryThing(TargetIndex.B, subtractNumTakenFromJobCount: true).FailOnDestroyedNullOrForbidden(TargetIndex.B);
			yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveFuel, TargetIndex.B, TargetIndex.None);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.Wait(RefuelingDuration, TargetIndex.None).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A).
				FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch).WithProgressBarToilDelay(TargetIndex.A);
			yield return FinalizeRefueling(TargetIndex.A, TargetIndex.B);
		}

		public static Toil FinalizeRefueling(TargetIndex refuelableInd, TargetIndex fuelInd)
		{
			Toil toil = new Toil();
			toil.initAction = delegate()
			{
				Job curJob = toil.actor.CurJob;
				VehiclePawn vehicle = curJob.GetTarget(refuelableInd).Thing as VehiclePawn;
				if (toil.actor.CurJob.placedThings.NullOrEmpty())
				{
					vehicle.CompFueledTravel.Refuel(new List<Thing> { curJob.GetTarget(fuelInd).Thing });
					return;
				}
				vehicle.CompFueledTravel.Refuel(toil.actor.CurJob.placedThings.Select(thing => thing.thing).ToList());
			};
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			return toil;
		}
	}
}
