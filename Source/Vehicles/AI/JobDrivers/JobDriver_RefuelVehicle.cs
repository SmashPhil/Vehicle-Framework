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
	public class JobDriver_RefuelVehicle : JobDriver
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
			this.FailOnMoving(TargetIndex.A);
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
			Toil reserveFuel = Toils_Reserve.Reserve(TargetIndex.B, 1, -1, null);
			yield return reserveFuel;
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
			yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true, false).FailOnDestroyedNullOrForbidden(TargetIndex.B);
			yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveFuel, TargetIndex.B, TargetIndex.None, true, null);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			yield return Toils_General.Wait(RefuelingDuration, TargetIndex.None).FailOnDestroyedNullOrForbidden(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.A).
				FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch).WithProgressBarToilDelay(TargetIndex.A, false, -0.5f);
			yield return FinalizeRefueling(TargetIndex.A, TargetIndex.B);
		}

		public static Toil FinalizeRefueling(TargetIndex refuelableInd, TargetIndex fuelInd)
		{
			Toil toil = new Toil();
			toil.initAction = delegate()
			{
				Job curJob = toil.actor.CurJob;
				Thing thing = curJob.GetTarget(refuelableInd).Thing;
				if (toil.actor.CurJob.placedThings.NullOrEmpty())
				{
					thing.TryGetComp<CompFueledTravel>().Refuel(new List<Thing>
					{
						curJob.GetTarget(fuelInd).Thing
					});
					return;
				}
				thing.TryGetComp<CompFueledTravel>().Refuel((from p in toil.actor.CurJob.placedThings
																select p.thing).ToList<Thing>());
			};
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			return toil;
		}
	}
}
