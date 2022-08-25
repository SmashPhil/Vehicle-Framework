using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using Verse.AI;

namespace Vehicles
{
	public class VehicleTargetReservation : Reservation<LocalTargetInfo>
	{
		private Dictionary<Pawn, LocalTargetInfo> claimants;

		private List<Pawn> pawnClaimants;
		private List<LocalTargetInfo> pawnTargets;

		public VehicleTargetReservation()
		{
		}

		public VehicleTargetReservation(VehiclePawn vehicle, Job job, int maxClaimants) : base(vehicle, job, maxClaimants)
		{
			claimants = new Dictionary<Pawn, LocalTargetInfo>();
		}

		public override int TotalClaimants => claimants.Count;

		public override bool RemoveNow => !claimants.Any();

		public override bool AddClaimant(Pawn pawn, LocalTargetInfo target)
		{
			if (claimants.ContainsKey(pawn))
			{
				Log.Error($"Attempting to reserve Vehicle with {pawn.LabelShort}. Target {target} is already reserved.");
				return false;
			}
			claimants[pawn] = target;
			return true;
		}

		public override bool CanReserve(Pawn pawn, LocalTargetInfo target)
		{
			return !claimants.ContainsKey(pawn) && !claimants.ContainsValue(target);
		}

		public override void ReleaseAllReservations()
		{
			foreach (Pawn p in claimants.Keys)
			{
				p.jobs.EndCurrentJob(JobCondition.InterruptForced);
				p.ClearMind();
			}
		}

		public override void ReleaseReservationBy(Pawn pawn)
		{
			if (claimants.ContainsKey(pawn))
			{
				claimants.Remove(pawn);
			}
		}

		public override void VerifyAndValidateClaimants()
		{
			List<Pawn> actors = new List<Pawn>(claimants.Keys);
			foreach (Pawn actor in actors)
			{
				//Fail if job def changes, vehicle target changes, targetInfo is no longer valid, or vehicle gets drafted
				if (actor == null || !actor.Spawned || actor.Dead || actor.CurJob.def.defName != jobDef || actor.CurJob.targetA != targetA || !claimants[actor].IsValid || actor.Drafted || vehicle.Drafted)
				{
					claimants.Remove(actor);
				}
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref claimants, nameof(claimants), LookMode.Reference, LookMode.LocalTargetInfo, ref pawnClaimants, ref pawnTargets);
		}
	}
}
