using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

public static class ExitMapUtility
{
  public static bool IsFollowingAnyone(Pawn pawn)
  {
    return pawn.mindState.duty.focus.HasThing;
  }

  public static void SetFollower(Pawn pawn, Pawn follower)
  {
    pawn.mindState.duty.focus = follower;
    pawn.mindState.duty.radius = 10f;
  }

  public static void CheckArrived(Lord lord, List<Pawn> pawnsToCheck, IntVec3 meetingPoint,
    string memo, Predicate<Pawn> shouldCheckIfArrived, Predicate<Pawn> extraValidator = null)
  {
    const int MaxDistanceFromVehicle = 15;

    VehiclePawn leadVehicle = ((LordJob_FormAndSendVehicles)lord.LordJob).LeadVehicle;
    Assert.IsNotNull(leadVehicle);
    foreach (Pawn pawn in pawnsToCheck)
    {
      if (!shouldCheckIfArrived(pawn))
        continue;

      if (!pawn.Spawned)
        return;

      if (pawn == leadVehicle)
      {
        if (!leadVehicle.Position.InHorDistOf(meetingPoint, leadVehicle.VehicleDef.Size.z))
          return;
        if (!leadVehicle.CanReachVehicle(meetingPoint, PathEndMode.ClosestTouch, Danger.Deadly))
          return;
      }
      else if (pawn.mindState?.duty?.focus.Thing is VehiclePawn vehicle)
      {
        if (!pawn.Position.InHorDistOf(vehicle.Position, MaxDistanceFromVehicle))
          return;
      }
      if (extraValidator != null && !extraValidator(pawn))
        return;
    }
    lord.ReceiveMemo(memo);
  }
}