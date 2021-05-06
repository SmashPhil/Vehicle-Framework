using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicles
{
	public abstract class Reservation<T> : ReservationBase
	{
		public Reservation()
		{
		}

		public Reservation(VehiclePawn vehicle, Job job, int maxClaimants) : base(vehicle, job, maxClaimants)
		{
		}

		public abstract bool AddClaimant(Pawn pawn, T target);

		public abstract bool CanReserve(Pawn pawn, T target);
	}
}
