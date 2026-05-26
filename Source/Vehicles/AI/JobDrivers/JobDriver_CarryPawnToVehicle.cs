using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

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

    Toil checkArrestResistance = ToilMaker.MakeToil();
    checkArrestResistance.initAction = delegate
    {
      if (!job.def.makeTargetPrisoner)
        return;

      PawnToCarry.GetLord()?.Notify_PawnAttemptArrested(PawnToCarry);
      GenClamor.DoClamor(PawnToCarry, 10f, ClamorDefOf.Harm);
      if (!PawnToCarry.IsPrisoner && !PawnToCarry.IsSlave)
      {
        QuestUtility.SendQuestTargetSignals(PawnToCarry.questTags,
          QuestUtility.QuestTargetSignalPart_Arrested, PawnToCarry.Named(SignalArgsNames.Subject));
        if (PawnToCarry.Faction != null)
        {
          QuestUtility.SendQuestTargetSignals(PawnToCarry.Faction.questTags,
            QuestUtility.QuestTargetSignalPart_FactionMemberArrested, PawnToCarry.Faction.Named(SignalArgsNames.Faction));
        }
      }
      if (job.def == JobDefOf.Arrest && !PawnToCarry.CheckAcceptArrest(pawn))
      {
        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
      }
    };

    Toil gotoVehicle = Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
    yield return Toils_Jump.JumpIf(checkArrestResistance, () => pawn.IsCarryingPawn(carryPawn: PawnToCarry));
    yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TargetIndex.A)
      .FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOn(() =>
        !PawnToCarry.Downed).FailOn(() => !pawn.CanReach(PawnToCarry, PathEndMode.OnCell, Danger.Deadly))
      .FailOnSomeonePhysicallyInteracting(TargetIndex.A);

    Toil startCarrying = Toils_Haul.StartCarryThing(TargetIndex.A);
    startCarrying.AddPreInitAction(CheckMakeTakeeGuest);
    startCarrying.AddFinishAction(() =>
    {
      if (pawn.Faction != PawnToCarry.Faction)
        return;

      CapturePawnAsPrisoner(PawnToCarry, capturedBy: pawn);
    });
    yield return startCarrying;
    yield return checkArrestResistance;
    yield return gotoVehicle;
    yield return Toils_General.Wait(WaitTicks).FailOnCannotTouch(TargetIndex.B, PathEndMode.Touch)
     .WithProgressBarToilDelay(TargetIndex.B);
    yield return PlaceInsideVehicleToil();
  }

  private Toil PlaceInsideVehicleToil()
  {
    Toil toil = new()
    {
      initAction = delegate
      {
        if (Vehicle.TryAddPawn(PawnToCarry))
        {
          CapturePawnAsPrisoner(PawnToCarry, capturedBy: pawn);
          PawnToCarry.playerSettings ??= new Pawn_PlayerSettings(PawnToCarry);
        }
      },
      defaultCompleteMode = ToilCompleteMode.Instant
    };
    return toil;
  }

  private void CheckMakeTakeeGuest()
  {
    if (job.def.makeTargetPrisoner)
      return;
    if (PawnToCarry.Faction == Faction.OfPlayer || PawnToCarry.HostFaction == Faction.OfPlayer)
      return;
    if (PawnToCarry.guest == null || PawnToCarry.IsWildMan())
      return;
    if (PawnToCarry.DevelopmentalStage == DevelopmentalStage.Baby)
      return;

    PawnToCarry.guest.SetGuestStatus(Faction.OfPlayer);
    QuestUtility.SendQuestTargetSignals(PawnToCarry.questTags, QuestUtility.QuestTargetSignalPart_Rescued,
      PawnToCarry.Named(SignalArgsNames.Subject));
  }

  private void CapturePawnAsPrisoner(Pawn target, Pawn capturedBy)
  {
    if (!job.def.makeTargetPrisoner)
      return;

    if (target.guest.Released)
    {
      target.guest.Released = false;
      target.guest.SetExclusiveInteraction(PrisonerInteractionModeDefOf.MaintainOnly);
      GenGuest.RemoveHealthyPrisonerReleasedThoughts(target);
    }
    if (!target.IsPrisonerOfColony)
    {
      target.guest.CapturedBy(Faction.OfPlayer, capturedBy);
    }
  }

  [Obsolete("Create your own toil, this will be removed in 1.7")]
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