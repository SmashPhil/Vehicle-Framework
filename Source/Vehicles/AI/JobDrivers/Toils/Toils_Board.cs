using System;
using RimWorld;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

internal class Toils_Board
{
  public static Toil BoardVehicle(Pawn pawn)
  {
    Toil toil = new();
    toil.initAction = delegate
    {
      VehiclePawn vehicle = toil.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing as VehiclePawn;
      Assert.IsNotNull(vehicle);
      if (pawn.GetLord()?.LordJob is IVehicleAssigner assigner)
      {
        AssignedSeat assignedSeat = assigner.GetAssignedSeat(pawn);
        assignedSeat.Vehicle.TryAddPawn(pawn, assignedSeat.handler);
        return;
      }
      vehicle.BoardPawn(pawn);
      ThrowAppropriateHistoryEvent(vehicle.VehicleDef.type, toil.actor);
    };
    toil.defaultCompleteMode = ToilCompleteMode.Instant;
    return toil;
  }

  private static void ThrowAppropriateHistoryEvent(VehicleType type, Pawn pawn)
  {
    if (ModsConfig.IdeologyActive)
    {
      switch (type)
      {
        case VehicleType.Air:
          Find.HistoryEventsManager.RecordEvent(new HistoryEvent(
            HistoryEventDefOf_Vehicles.VF_BoardedAirVehicle,
            pawn.Named(HistoryEventArgsNames.Doer)));
        break;
        case VehicleType.Sea:
          Find.HistoryEventsManager.RecordEvent(new HistoryEvent(
            HistoryEventDefOf_Vehicles.VF_BoardedSeaVehicle,
            pawn.Named(HistoryEventArgsNames.Doer)));
        break;
        case VehicleType.Land:
          Find.HistoryEventsManager.RecordEvent(new HistoryEvent(
            HistoryEventDefOf_Vehicles.VF_BoardedLandVehicle,
            pawn.Named(HistoryEventArgsNames.Doer)));
        break;
        case VehicleType.Universal:
          Find.HistoryEventsManager.RecordEvent(new HistoryEvent(
            HistoryEventDefOf_Vehicles.VF_BoardedUniversalVehicle,
            pawn.Named(HistoryEventArgsNames.Doer)));
        break;
        default:
          throw new NotImplementedException(nameof(VehicleType));
      }
    }
  }
}