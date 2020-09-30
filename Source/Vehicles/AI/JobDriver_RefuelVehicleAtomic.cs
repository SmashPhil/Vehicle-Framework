using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace Vehicles.Jobs
{
    public class JobDriver_RefuelVehicleAtomic : JobDriver
	{

		protected Thing Refuelable
		{
			get
			{
				return job.GetTarget(RefuelableInd).Thing;
			}
		}

		protected CompFueledTravel RefuelableComp
		{
			get
			{
				return Refuelable.TryGetComp<CompFueledTravel>();
			}
		}

		protected Thing Fuel
		{
			get
			{
				return job.GetTarget(FuelInd).Thing;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
            return pawn.Reserve(Refuelable, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Fuel, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(RefuelableInd);
			/*base.AddEndCondition(delegate
			{
				if (!this.RefuelableComp.FullTank)
				{
					return JobCondition.Ongoing;
				}
				return JobCondition.Succeeded;
			});*/
			yield return Toils_General.DoAtomic(delegate
			{
				job.count = RefuelableComp.FuelCountToFull;
			});
			Toil reserveFuel = Toils_Reserve.Reserve(FuelInd, 1, -1, null);
			yield return reserveFuel;
			yield return Toils_Goto.GotoThing(FuelInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(FuelInd).FailOnSomeonePhysicallyInteracting(FuelInd);
			yield return Toils_Haul.StartCarryThing(FuelInd, false, true, false).FailOnDestroyedNullOrForbidden(FuelInd);
			yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveFuel, FuelInd, TargetIndex.None, true, null);
			yield return Toils_Goto.GotoThing(RefuelableInd, PathEndMode.Touch);
			yield return Toils_General.Wait(RefuelingDuration, TargetIndex.None).FailOnDestroyedNullOrForbidden(FuelInd).FailOnDestroyedNullOrForbidden(RefuelableInd).
                FailOnCannotTouch(RefuelableInd, PathEndMode.Touch).WithProgressBarToilDelay(RefuelableInd, false, -0.5f);
			yield return FinalizeRefueling(RefuelableInd, FuelInd);
			yield break;
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

		private const TargetIndex RefuelableInd = TargetIndex.A;
		private const TargetIndex FuelInd = TargetIndex.B;
		private const int RefuelingDuration = 240;
	}
}
