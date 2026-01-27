using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class JobDriver_CarryPawnToVehicle : JobDriver
{
  // TODO 1.7 - Remove
  [Obsolete("No longer in use. Will be removed in 1.7", error: true)]
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

  public VehiclePawn Vehicle => job.GetTarget(TargetIndex.B).Thing as VehiclePawn;

  // TODO 1.7 - Remove
  [Obsolete("Use PawnToCarry instead.")]
  public Pawn Pawn => (Pawn)job.GetTarget(TargetIndex.A).Thing;

  public Pawn PawnToCarry => job.GetTarget(TargetIndex.A).Thing as Pawn;

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    LocalTargetInfo target = job.GetTarget(TargetIndex.A);
    return pawn.Reserve(target, job, errorOnFailed: errorOnFailed);
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    const int WaitTicks = 30;

    this.FailOnDestroyedOrNull(TargetIndex.A);
    this.FailOnDestroyedOrNull(TargetIndex.B);
    this.FailOnAggroMentalState(TargetIndex.A);
    this.FailOnBurningImmobile(TargetIndex.B);
    this.FailOnMoving(TargetIndex.B);

    Toil gotoVehicle = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
    yield return Toils_Jump.JumpIf(gotoVehicle, () => pawn.IsCarryingPawn(carryPawn: PawnToCarry));
    yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A)
      .FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOn(() =>
        !PawnToCarry.Downed).FailOn(() => !pawn.CanReach(PawnToCarry, PathEndMode.OnCell, Danger.Deadly))
      .FailOnSomeonePhysicallyInteracting(TargetIndex.A);
    yield return Toils_Haul.StartCarryThing(TargetIndex.A);
    yield return gotoVehicle;
    yield return Toils_General.Wait(WaitTicks).FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch)
     .WithProgressBarToilDelay(TargetIndex.B);
    yield return PutPawnOnVehicle(PawnToCarry, Vehicle);
  }

  public static Toil PutPawnOnVehicle(Pawn pawn, VehiclePawn vehicle)
  {
    Toil toil = new()
    {
      initAction = delegate
      {
        vehicle.TryAddPawn(pawn);
      },
      defaultCompleteMode = ToilCompleteMode.Instant
    };
    return toil;
  }
}