using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace Vehicles
{
	public class VehicleHandlerReservation : Reservation<VehicleHandler>
	{
		private Dictionary<Pawn, VehicleHandler> claimants;
		private Dictionary<VehicleHandler, int> handlerClaimants;

		private List<VehicleHandler> pawnClaimants = new List<VehicleHandler>();
		private List<int> claimantCounts = new List<int>();

		private static readonly List<Pawn> removeActors = new List<Pawn>();

		public VehicleHandlerReservation()
		{
		}

		public VehicleHandlerReservation(VehiclePawn vehicle, Job job, int maxClaimants) : base(vehicle, job, maxClaimants)
		{
			claimants = new Dictionary<Pawn, VehicleHandler>();
			handlerClaimants = new Dictionary<VehicleHandler, int>();
		}

		public override int TotalClaimants => claimants.Count;

		public override bool RemoveNow => !claimants.Any();

		public int ClaimantsOnHandler(VehicleHandler handler) => claimants.Where(c => c.Value == handler).Count();

		public VehicleHandler ReservedHandler(Pawn pawn) => claimants.TryGetValue(pawn);

		public override bool AddClaimant(Pawn pawn, VehicleHandler target)
		{
			if (claimants.ContainsKey(pawn))
			{
				Log.Error($"Attempting to reserve Vehicle with {pawn.LabelShort}. Handler {target} is already reserved.");
				return false;
			}
			claimants[pawn] = target;
			if (handlerClaimants.ContainsKey(target))
			{
				handlerClaimants[target]++;
			}
			else
			{
				handlerClaimants[target] = 1;
			}
			return true;
		}

		public override bool CanReserve(Pawn pawn, VehicleHandler target, StringBuilder stringBuilder = null)
		{
			int reservations = handlerClaimants.TryGetValue(target, 0);
			bool rolesAvailable = (target.handlers.Count + reservations) < target.role.Slots;
			if (!rolesAvailable)
			{
				stringBuilder?.AppendLine($"Roles not available.  Existing={target.handlers.Count} Claimants={string.Join(",", claimants.Where(kvp => kvp.Value == target).Select(kvp => kvp.Key))} Allowed: {target.role.Slots}");
				return false;
			}
			if (pawn is null)
			{
				stringBuilder?.AppendLine("Null Claimant");
				return true;
			}
			bool newClaimant = !claimants.ContainsKey(pawn);
			stringBuilder?.AppendLine($"{pawn} is new claimant? {newClaimant}");
			return newClaimant;
		}

		public override bool ReservedBy(Pawn pawn, VehicleHandler target)
		{
			return claimants.TryGetValue(pawn, out VehicleHandler handler) && handler == target;
		}

		public override void ReleaseAllReservations()
		{
			foreach (Pawn pawn in claimants.Keys)
			{
				if (pawn?.jobs != null)
				{
					pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
					pawn.ClearMind();
				}
			}
		}

		public override void ReleaseReservationBy(Pawn pawn)
		{
			if (claimants.ContainsKey(pawn))
			{
				if(--handlerClaimants[claimants[pawn]] <= 0)
				{
					handlerClaimants.Remove(claimants[pawn]);
				}
				claimants.Remove(pawn);
			}
		}

		public override void VerifyAndValidateClaimants()
		{
			removeActors.Clear();
			foreach (Pawn actor in claimants.Keys)
			{
				Job matchedJob = actor.CurJob;
				if (actor?.jobs != null && actor.CurJob?.def != jobDef)
				{
					matchedJob = actor.jobs.jobQueue?.FirstOrDefault(j => j.job.def == jobDef)?.job;
				}

				if (!actor.Spawned || actor.InMentalState || actor.Downed || actor.Dead || matchedJob?.def != jobDef || matchedJob?.targetA != targetA)
				{
					if (--handlerClaimants[claimants[actor]] <= 0)
					{
						handlerClaimants.Remove(claimants[actor]);
					}
					removeActors.Add(actor);
				}
			}
			foreach (Pawn removeActor in removeActors)
			{
				claimants.Remove(removeActor);
			}
			removeActors.Clear();
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref claimants, "claimants", LookMode.Reference, LookMode.Reference);
			Scribe_Collections.Look(ref handlerClaimants, "handlerClaimants", LookMode.Reference, LookMode.Value, ref pawnClaimants, ref claimantCounts);
		}
	}
}
