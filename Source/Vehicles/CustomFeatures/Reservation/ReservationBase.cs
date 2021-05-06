using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public abstract class ReservationBase : IExposable
	{
		protected VehiclePawn vehicle;

		protected string jobDef;

		protected LocalTargetInfo targetA;

		protected int maxClaimants;

		private int uniqueId = -1;

		public ReservationBase()
		{
		}

		public ReservationBase(VehiclePawn vehicle, Job job, int maxClaimants)
		{
			this.vehicle = vehicle;
			jobDef = job.def.defName;
			targetA = job.targetA;
			this.maxClaimants = maxClaimants;
			uniqueId = Current.Game.GetCachedGameComponent<VehicleIdManager>().GetNextReservationId();
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

		//public string GetUniqueLoadID()
		//{
		//    return $"{GetType()}_{uniqueId}";
		//}
	}
}
