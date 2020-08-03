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
    public class JobDriver_RefuelBoat : JobDriver
	{

		protected Thing Refuelable
		{
			get
			{
				return this.job.GetTarget(RefuelableInd).Thing;
			}
		}

		protected CompFueledTravel RefuelableComp
		{
			get
			{
				return this.Refuelable.TryGetComp<CompFueledTravel>();
			}
		}

		protected Thing Fuel
		{
			get
			{
				return this.job.GetTarget(FuelInd).Thing;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
            return this.pawn.Reserve(this.Refuelable, this.job, 1, -1, null, errorOnFailed) && this.pawn.Reserve(this.Fuel, this.job, 1, -1, null, errorOnFailed);
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
				this.job.count = this.RefuelableComp.FuelCountToFull;
			});
			Toil reserveFuel = Toils_Reserve.Reserve(FuelInd, 1, -1, null);
			yield return reserveFuel;
			yield return Toils_Goto.GotoThing(FuelInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(FuelInd).FailOnSomeonePhysicallyInteracting(FuelInd);
			yield return Toils_Haul.StartCarryThing(FuelInd, false, true, false).FailOnDestroyedNullOrForbidden(FuelInd);
			yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserveFuel, FuelInd, TargetIndex.None, true, null);
			yield return Toils_Goto.GotoThing(RefuelableInd, PathEndMode.Touch);
			yield return Toils_General.Wait(RefuelingDuration, TargetIndex.None).FailOnDestroyedNullOrForbidden(FuelInd).FailOnDestroyedNullOrForbidden(RefuelableInd).
                FailOnCannotTouch(RefuelableInd, PathEndMode.Touch).WithProgressBarToilDelay(RefuelableInd, false, -0.5f);
			yield return JobDriver_RefuelBoat.FinalizeRefueling(RefuelableInd, FuelInd);
			yield break;
		}

        public static Toil FinalizeRefueling(TargetIndex refuelableInd, TargetIndex fuelInd)
		{
			Toil toil = new Toil();
			toil.initAction = delegate()
			{
				Job curJob = toil.actor.CurJob;
				Thing thing = curJob.GetTarget(refuelableInd).Thing;
                if (toil.actor.CurJob.placedThings.NullOrEmpty<ThingCountClass>())
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
    /*public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
            this.pawn.ReserveAsManyAsPossible(job.GetTargetQueue(FuelInd), job, 1, -1, null);
            return pawn.Reserve(Refuelable, job, 1, -1, null, errorOnFailed);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(RefuelableInd);
			base.AddEndCondition(delegate
			{
				if (!this.RefuelableComp.FullTank)
				{
					return JobCondition.Ongoing;
				}
				return JobCondition.Succeeded;
			});
			yield return Toils_General.DoAtomic(delegate
			{
				this.job.count = this.RefuelableComp.FuelCountToFull;
			});
			Toil getNextIngredient = Toils_General.Label();
			yield return getNextIngredient;
			yield return Toils_JobTransforms.ExtractNextTargetFromQueue(TargetIndex.B, true);
			yield return Toils_Goto.GotoThing(FuelInd, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(FuelInd).FailOnSomeonePhysicallyInteracting(FuelInd);
			yield return Toils_Haul.StartCarryThing(FuelInd, false, true, false).FailOnDestroyedNullOrForbidden(FuelInd);
			yield return Toils_Goto.GotoThing(RefuelableInd, PathEndMode.Touch);
            yield return Toils_Jump.JumpIf(getNextIngredient, () => !this.job.GetTargetQueue(TargetIndex.B).NullOrEmpty<LocalTargetInfo>());
			yield return Toils_General.Wait(RefuelingDuration, TargetIndex.None).FailOnDestroyedNullOrForbidden(FuelInd).FailOnDestroyedNullOrForbidden(RefuelableInd).
                FailOnCannotTouch(RefuelableInd, PathEndMode.Touch).WithProgressBarToilDelay(RefuelableInd, false, -0.5f);
			yield return JobDriver_RefuelBoat.FinalizeRefueling(RefuelableInd, FuelInd);
			yield break;
		}*/
}
