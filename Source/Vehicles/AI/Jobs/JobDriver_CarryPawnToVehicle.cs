using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace Vehicles
{
	public class JobDriver_CarryPawnToVehicle : JobDriver
	{
		public VehiclePawn Vehicle => job.GetTarget(TargetIndex.B).Thing as VehiclePawn;

		public VehicleHandler VehicleHandler
		{
			get
			{
				if (job is Job_Vehicle jobVehicle)
				{
					return jobVehicle.handler;
				}
				VehicleHandler operationalHandler = Vehicle.handlers.FirstOrDefault(handler => handler.CanOperateRole(Pawn));
				if (operationalHandler == null)
				{
					operationalHandler = Vehicle.handlers.FirstOrDefault(handler => handler.CanOperateRole(Pawn));
				}
				return operationalHandler;
			}
		}

		public Pawn Pawn => (Pawn)job.GetTarget(TargetIndex.A).Thing;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			Pawn pawn = this.pawn;
			LocalTargetInfo target = this.job.GetTarget(TargetIndex.A);
			Job job = this.job;
			return pawn.Reserve(target, job, 1, -1, null, errorOnFailed); 
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(TargetIndex.A);
			this.FailOnDestroyedOrNull(TargetIndex.B);
			this.FailOnAggroMentalState(TargetIndex.A);
			this.FailOnBurningImmobile(TargetIndex.B);

			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOn(() =>
				!Pawn.Downed).FailOn(() => !pawn.CanReach(Pawn, PathEndMode.OnCell, Danger.Deadly, false)).FailOnSomeonePhysicallyInteracting(TargetIndex.A);
			yield return Toils_Haul.StartCarryThing(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
			yield return Toils_General.Wait(250, TargetIndex.None).FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch).WithProgressBarToilDelay(TargetIndex.B, false, -0.5f);
			
			yield return PutPawnOnVehicle(Pawn, Vehicle, VehicleHandler);
		}

		public static Toil PutPawnOnVehicle(Pawn pawn, VehiclePawn vehicle, VehicleHandler handler)
		{
			Toil toil = new Toil();
			toil.initAction = delegate ()
			{
				vehicle.GiveLoadJob(pawn, handler);
				vehicle.Notify_Boarded(pawn);
			};
			toil.defaultCompleteMode = ToilCompleteMode.Instant;
			return toil;
		}
	}
}
