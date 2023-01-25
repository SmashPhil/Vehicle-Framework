using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public abstract class ReservationBase : IExposable
	{
		protected VehiclePawn vehicle;
		protected JobDef jobDef;
		protected LocalTargetInfo targetA;

		protected int maxClaimants;

		private int uniqueId = -1;

		public ReservationBase()
		{
		}

		public ReservationBase(VehiclePawn vehicle, Job job, int maxClaimants)
		{
			this.vehicle = vehicle;
			jobDef = job.def;
			targetA = job.targetA;
			this.maxClaimants = maxClaimants;
			uniqueId = VehicleIdManager.Instance.GetNextReservationId();
		}

		public abstract bool RemoveNow { get; }

		public abstract int TotalClaimants { get; }

		public JobDef JobDef => jobDef;

		public VehiclePawn Vehicle => vehicle;

		public LocalTargetInfo TargetA => targetA;

		public abstract void ReleaseReservationBy(Pawn pawn);

		public abstract void VerifyAndValidateClaimants();

		public abstract void ReleaseAllReservations();

		public override string ToString()
		{
			return $"{GetType()} : {vehicle.LabelShort}";
		}

		public virtual void ExposeData()
		{
			Scribe_References.Look(ref vehicle, nameof(vehicle), true);
			Scribe_TargetInfo.Look(ref targetA, nameof(targetA));
			Scribe_Defs.Look(ref jobDef, nameof(jobDef));
			Scribe_Values.Look(ref maxClaimants, nameof(maxClaimants));
		}

		//public string GetUniqueLoadID()
		//{
		//    return $"{GetType()}_{uniqueId}";
		//}
	}
}
