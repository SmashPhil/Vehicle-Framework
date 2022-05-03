using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicles
{
	[Obsolete("Incomplete")]
	public class JobDriver_RopeAnimalToVehicle : JobDriver_RopeToDestination
	{
		protected virtual int TicksToRope => 60;

		public Pawn Animal => job.GetTarget(TargetIndex.A).Pawn;

		public VehiclePawn Vehicle => job.GetTarget(TargetIndex.B).Pawn as VehiclePawn;

		public IntVec3 RopingCell => job.GetTarget(TargetIndex.C).Cell;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			if (!pawn.roping.Ropees.NullOrEmpty())
			{
				foreach (Pawn ropee in pawn.roping.Ropees)
				{
					pawn.Reserve(ropee, job, errorOnFailed: errorOnFailed);
				}
			}
			UpdateDestination();
			return pawn.Reserve(Animal, job, errorOnFailed: errorOnFailed);
		}

		protected override bool ShouldOpportunisticallyRopeAnimal(Pawn animal)
		{
			return JobGiver_PrepareCaravan_CollectPawns.DoesAnimalNeedGathering(pawn, animal);
		}

		protected override bool HasRopeeArrived(Pawn ropee, bool roperWaitingAtDest)
		{
			PawnDuty duty = pawn.mindState.duty;
			LocalTargetInfo vehicleTarget = (duty != null) ? duty.focus : LocalTargetInfo.Invalid;
			if (!vehicleTarget.IsValid || !(vehicleTarget.Pawn is VehiclePawn))
			{
				return false;
			}
			VehiclePawn vehicle = vehicleTarget.Pawn as VehiclePawn;
			if (!pawn.Position.InHorDistOf(RopingCell, 2))
			{
				return false;
			}
			District district = vehicle.Position.GetDistrict(vehicle.Map, RegionType.Set_Passable);
			bool sameDistrict = district == vehicle.GetDistrict(RegionType.Set_Passable) && district == ropee.GetDistrict(RegionType.Set_Passable);
			return sameDistrict;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield return Toils_General.Do(delegate
			{
				UpdateDestination();
			});
			AddFinishAction(delegate
			{
				pawn?.roping?.DropRopes();
			});
			Toil findAnotherAnimal = Toils_General.Label();
			Toil topOfLoop = Toils_General.Label();
			yield return topOfLoop;
			yield return Toils_Jump.JumpIf(findAnotherAnimal, delegate
			{
				return Animal?.roping.RopedByPawn == pawn;
			});
			//Reserve and rope animal to pawn
			yield return Toils_Reserve.Reserve(TargetIndex.A);
			yield return Toils_Rope.GotoRopeAttachmentInteractionCell(TargetIndex.A);
			yield return Toils_Rope.RopePawn(TargetIndex.A);

			Toil gotoToil = Toils_Goto.GotoCell(RopingCell, PathEndMode.Touch);
			MatchLocomotionUrgency(gotoToil);
			gotoToil.AddPreTickAction(delegate
			{
				ProcessRopeesThatHaveArrived(false);
			});
			gotoToil.FailOn(() =>
			{
				return !pawn.roping.IsRopingOthers;
			});
			yield return gotoToil;
			
			//Transfer rope from pawn to vehicle
			yield return TransferRope(TargetIndex.A, TargetIndex.B);

			//Loop logic until out of animals
			yield return findAnotherAnimal;
			yield return Toils_Jump.JumpIf(topOfLoop, FindAnotherAnimalToRope);
			topOfLoop = Toils_General.Label();
			yield return topOfLoop;
			
			yield return Toils_Jump.JumpIf(topOfLoop, UpdateDestination);
			topOfLoop = Toils_General.Wait(TicksToRope, TargetIndex.A);
			topOfLoop.AddPreTickAction(delegate
			{
				ProcessRopeesThatHaveArrived(true);
			});
			yield return topOfLoop;
			yield return Toils_Jump.JumpIf(topOfLoop, () => pawn.roping.IsRopingOthers);
		}

		public static Toil TransferRope(TargetIndex ropeeIndex, TargetIndex targetIndex)
		{
			Toil toil = new Toil();
			toil.initAction = delegate ()
			{
				Pawn actor = toil.actor;
				Pawn transferee = actor.jobs.curJob.GetTarget(targetIndex).Pawn;
				Pawn pawn = actor.jobs.curJob.GetTarget(ropeeIndex).Thing as Pawn;
				if (pawn != null)
				{
					transferee.roping.RopePawn(pawn);
					Pawn_CallTracker caller = pawn.caller;
					if (caller != null)
					{
						caller.DoCall();
					}
					PawnUtility.ForceWait(pawn, 30, actor);
				}
			};
			toil.defaultCompleteMode = ToilCompleteMode.Delay;
			toil.defaultDuration = 30;
			toil.FailOnDespawnedOrNull(ropeeIndex);
			toil.FailOnDespawnedOrNull(targetIndex);
			toil.PlaySustainerOrSound(() => SoundDefOf.Roping);
			return toil;
		}

		protected virtual void ProcessRopeesThatHaveArrived(bool roperWaitingAtDest)
		{
			for (int i = pawn.roping.Ropees.Count - 1; i >= 0; i--)
			{
				Pawn ropee = pawn.roping.Ropees[i];
				if (HasRopeeArrived(pawn, roperWaitingAtDest))
				{
					pawn.roping.DropRope(ropee);
					if (ropee.jobs != null && ropee.CurJob != null && ropee.jobs.curDriver is JobDriver_FollowRoper)
					{
						ropee.jobs.EndCurrentJob(JobCondition.InterruptForced);
					}
					ProcessArrivedRopee(pawn);
				}
			}
		}

		protected virtual void MatchLocomotionUrgency(Toil toil)
		{
			toil.AddPreInitAction(delegate
			{
				locomotionUrgencySameAs = SlowestRopee();
			});
			toil.AddFinishAction(delegate
			{
				locomotionUrgencySameAs = null;
			});
		}

		protected virtual Pawn SlowestRopee()
		{
			Pawn result;
			if (!pawn.roping.Ropees.TryMaxBy((Pawn p) => p.TicksPerMoveCardinal, out result))
			{
				return null;
			}
			return result;
		}

		protected virtual bool FindAnotherAnimalToRope()
		{
			int limit = pawn.mindState?.duty?.ropeeLimit ?? 10;
			if (pawn.roping.Ropees.Count >= limit)
			{
				return false;
			}
			Thing thing = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.Pawn), PathEndMode.Touch, 
				TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false, false, false), 10f, (thing) => thing is Pawn pawn && ShouldOpportunisticallyRopeAnimal(pawn));
			if (thing == null)
			{
				thing = FindDistantAnimalToRope();
			}
			if (thing != null)
			{
				job.SetTarget(TargetIndex.A, thing);
				return true;
			}
			return false;
		}

		protected override void ProcessArrivedRopee(Pawn ropee)
		{
			PawnDuty duty = ropee.mindState.duty;
			LocalTargetInfo vehicleTarget = (duty != null) ? duty.focus : LocalTargetInfo.Invalid;
			if (vehicleTarget.IsValid)
			{
				ropee.roping.RopePawn(vehicleTarget.Pawn);
			}
		}

		public static Toil GotoRopeAttachmentInteractionCellForVehicle(IntVec3 ropingCell, TargetIndex ropeeIndex, TargetIndex vehicleIndex)
		{
			Toil toil = new Toil();
			toil.initAction = delegate ()
			{
				Pawn actor = toil.actor;
				Pawn ropee = actor.CurJob.GetTarget(ropeeIndex).Pawn;
				VehiclePawn vehicle = actor.CurJob.GetTarget(vehicleIndex).Pawn as VehiclePawn;
				if (!ropingCell.IsValid)
				{
					actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
				}
				if (actor.Position == ropingCell)
				{
					actor.jobs.curDriver.ReadyForNextToil();
					return;
				}
				actor.Map.debugDrawer.FlashCell(ropingCell);
				actor.pather.StartPath(ropingCell, PathEndMode.OnCell);
			};
			toil.tickAction = delegate ()
			{
				Pawn actor = toil.actor;
				Pawn ropee = actor.CurJob.GetTarget(ropeeIndex).Pawn;
				VehiclePawn vehicle = actor.CurJob.GetTarget(vehicleIndex).Pawn as VehiclePawn;
				bool badCell = !AnimalPenUtility.IsGoodRopeAttachmentInteractionCell(actor, ropee, actor.pather.Destination.Cell);
				if (actor.pather.Moving && badCell)
				{
					actor.Map.debugDrawer.FlashCell(actor.pather.Destination.Cell);
					ropingCell = vehicle.SurroundingCells.FirstOrDefault(cell => cell.IsValid && cell.WalkableBy(ropee.Map, ropee) && cell.WalkableBy(actor.Map, actor));
					if (ropingCell.IsValid)
					{
						actor.CurJob.SetTarget(TargetIndex.C, ropingCell);
						actor.pather.StartPath(ropingCell, PathEndMode.OnCell);
						return;
					}
					actor.jobs.curDriver.EndJobWith(JobCondition.Incompletable);
				}
			};
			toil.defaultCompleteMode = ToilCompleteMode.PatherArrival;
			toil.FailOnDespawnedOrNull(ropeeIndex);
			return toil;
		}
	}
}
