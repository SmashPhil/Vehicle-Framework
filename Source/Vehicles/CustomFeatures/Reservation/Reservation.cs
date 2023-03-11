using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		public abstract bool CanReserve(Pawn pawn, T target, StringBuilder stringBuilder = null);

		public abstract bool ReservedBy(Pawn pawn, T target);
	}
}
