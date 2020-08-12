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
            this.jobDef = job.def.defName;
            this.targetA = job.targetA;
            this.maxClaimants = maxClaimants;
        }

        public abstract bool RemoveNow { get; }

        public abstract int TotalClaimants { get; }

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
            Scribe_TargetInfo.Look(ref targetA, "targetA");
            Scribe_Values.Look(ref jobDef, "jobDef");
            Scribe_Values.Look(ref maxClaimants, "maxClaimants");
        }

        protected VehiclePawn vehicle;

        protected string jobDef;

        protected LocalTargetInfo targetA;

        protected int maxClaimants;
    }
}
