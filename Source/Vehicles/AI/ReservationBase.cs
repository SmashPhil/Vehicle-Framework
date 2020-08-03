using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace Vehicles
{
    public abstract class ReservationBase : IExposable
    {
        public ReservationBase(VehiclePawn vehicle, Job job, int maxClaimants)
        {
            this.vehicle = vehicle;
            this.job = job;
            this.maxClaimants = maxClaimants;
        }

        public abstract int TotalClaimants { get; }

        public abstract bool CanReserve(Pawn pawn);

        public abstract void ReleaseReservationBy(Pawn pawn);

        public abstract void VerifyAndValidateClaimants();

        public abstract void ReleaseAllReservations();

        public override string ToString()
        {
            return $"{GetType()} : {vehicle.LabelShort}";
        }

        public virtual void ExposeData()
        {
            Scribe_References.Look(ref vehicle, "vehicle");
            Scribe_References.Look(ref job, "job");
            Scribe_Values.Look(ref maxClaimants, "maxClaimants");
        }

        protected VehiclePawn vehicle;

        protected Job job;

        protected int maxClaimants;
    }
}
