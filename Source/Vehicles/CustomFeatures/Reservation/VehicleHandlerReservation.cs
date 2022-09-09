using System;
using System.Collections.Generic;
using System.Linq;
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
			if(claimants.ContainsKey(pawn))
			{
				Log.Error($"Attempting to reserve Vehicle with {pawn.LabelShort}. Handler {target} is already reserved.");
				return false;
			}
			claimants.Add(pawn, target);
			if(handlerClaimants.ContainsKey(target))
			{
				handlerClaimants[target]++;
			}
			else
			{
				handlerClaimants.Add(target, 1);
			}
			return true;
		}

		public override bool CanReserve(Pawn pawn, VehicleHandler target)
		{
			bool flag = (target.handlers.Count + (handlerClaimants.TryGetValue(target, out int claiming) ? claiming : 0)) < target.role.slots;
			return (pawn is null || !claimants.ContainsKey(pawn)) && flag;
		}

		public override void ReleaseAllReservations()
		{
			foreach(Pawn p in claimants.Keys)
			{
				p.jobs.EndCurrentJob(JobCondition.InterruptForced);
				p.ClearMind();
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
			List<Pawn> actors = new List<Pawn>(claimants.Keys);
			foreach (Pawn actor in actors)
			{
				Job matchedJob = actor.CurJob;
				if (matchedJob?.def.defName != jobDef)
				{
					matchedJob = actor.jobs.jobQueue?.FirstOrDefault(j => j.job.def.defName == jobDef)?.job;
				}
				if (!actor.Spawned || actor.InMentalState || actor.Downed || actor.Dead || matchedJob?.def.defName != jobDef || matchedJob?.targetA != targetA || vehicle.vPather.Moving)
				{
					if (--handlerClaimants[claimants[actor]] <= 0)
					{
						handlerClaimants.Remove(claimants[actor]);
					}
					claimants.Remove(actor);
				}
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Collections.Look(ref claimants, "claimants", LookMode.Reference, LookMode.Reference);
			Scribe_Collections.Look(ref handlerClaimants, "handlerClaimants", LookMode.Reference, LookMode.Value, ref pawnClaimants, ref claimantCounts);
		}
	}
}
