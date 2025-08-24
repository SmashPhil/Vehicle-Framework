using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class JobDriver_CarryPawnToVehicle : JobDriver
{
	public VehiclePawn Vehicle => job.GetTarget(TargetIndex.B).Thing as VehiclePawn;

	public VehicleRoleHandler VehicleHandler
	{
		get
		{
			if (job is Job_Vehicle jobVehicle)
			{
				return jobVehicle.handler;
			}
			VehicleRoleHandler operationalHandler = Vehicle.handlers.FirstOrDefault(handler => handler.CanOperateRole(Pawn));
			operationalHandler ??= Vehicle.handlers.FirstOrDefault(handler => handler.CanOperateRole(Pawn));
			return operationalHandler;
		}
	}

	public Pawn Pawn => (Pawn)job.GetTarget(TargetIndex.A).Thing;

	public override bool TryMakePreToilReservations(bool errorOnFailed)
	{
		LocalTargetInfo target = job.GetTarget(TargetIndex.A);
		return pawn.Reserve(target, job, 1, -1, null, errorOnFailed);
	}

	protected override IEnumerable<Toil> MakeNewToils()
	{
		this.FailOnDestroyedOrNull(TargetIndex.A);
		this.FailOnDestroyedOrNull(TargetIndex.B);
		this.FailOnAggroMentalState(TargetIndex.A);
		this.FailOnBurningImmobile(TargetIndex.B);

		yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A)
		 .FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOn(() =>
				!Pawn.Downed).FailOn(() => !pawn.CanReach(Pawn, PathEndMode.OnCell, Danger.Deadly))
		 .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
		yield return Toils_Haul.StartCarryThing(TargetIndex.A);
		yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
		yield return Toils_General.Wait(250).FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch)
		 .WithProgressBarToilDelay(TargetIndex.B);

		yield return PutPawnOnVehicle(Pawn, Vehicle, VehicleHandler);
	}

	public static Toil PutPawnOnVehicle(Pawn pawn, VehiclePawn vehicle, VehicleRoleHandler handler)
	{
		Toil toil = new()
		{
			initAction = delegate { vehicle.TryAddPawn(pawn, handler); },
			defaultCompleteMode = ToilCompleteMode.Instant
		};
		return toil;
	}
}