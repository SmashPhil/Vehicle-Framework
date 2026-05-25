using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

[UsedWithReflection]
public class JobGiver_CarryPawnToVehicle : ThinkNode_JobGiver
{
  protected override Job TryGiveJob(Pawn pawn)
  {
    if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
      return null;

    if (pawn.GetLord().LordJob is not LordJob_FormAndSendVehicles lordJob)
      return null;

    if (FindDownedPawn(pawn) is not { } downedPawn)
      return null;

    AssignedSeat assignedSeat = lordJob.GetAssignedSeat(downedPawn);
    if (assignedSeat is null)
    {
      VehicleRoleHandler handler = FindAvailableVehicle(downedPawn);
      if (handler is not null)
        assignedSeat = new AssignedSeat(pawn, handler);
    }
    if (assignedSeat is null)
    {
      Log.ErrorOnce(
        $"Unable to locate assigned or available vehicle for {downedPawn} in Caravan. Removing from caravan.",
        lordJob.GetHashCode());
      lordJob.lord.RemovePawn(downedPawn);
      return null;
    }
    Job_Vehicle job =
      new(JobDefOf_Vehicles.CarryPawnToVehicle, downedPawn, assignedSeat.Vehicle)
      {
        handler = assignedSeat.handler,
        count = 1
      };
    return job;
  }

  private static Pawn FindDownedPawn(Pawn pawn)
  {
    Lord lord = pawn.GetLord();
    List<Pawn> downedPawns = ((LordJob_FormAndSendVehicles)lord.LordJob).downedPawns;
    foreach (Pawn comatose in downedPawns)
    {
      if (comatose.Downed && comatose != pawn && comatose.Spawned)
      {
        if (pawn.CanReserveAndReach(comatose, PathEndMode.Touch, Danger.Deadly))
        {
          return comatose;
        }
      }
    }
    return null;
  }

  private static VehicleRoleHandler FindAvailableVehicle(Pawn pawn)
  {
    Lord lord = pawn.GetLord();
    LordJob_FormAndSendVehicles lordJob = (LordJob_FormAndSendVehicles)lord.LordJob;
    foreach (VehiclePawn vehicle in lordJob.vehicles)
    {
      foreach (VehicleRoleHandler handler in vehicle.handlers)
      {
        if (handler.CanOperateRole(pawn) && !lordJob.SeatAssigned(vehicle, handler))
        {
          return handler;
        }
      }
    }
    return null;
  }
}