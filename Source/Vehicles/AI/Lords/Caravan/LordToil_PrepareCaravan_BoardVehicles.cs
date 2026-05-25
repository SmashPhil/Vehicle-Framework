using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

public class LordToil_PrepareCaravan_BoardVehicles : LordToil, IDebugLordMeetingPoint
{
  private readonly IntVec3 meetingPoint;

  public LordToil_PrepareCaravan_BoardVehicles(IntVec3 meetingPoint)
  {
    this.meetingPoint = meetingPoint;
  }

  public IntVec3 MeetingPoint => meetingPoint;

  public override float? CustomWakeThreshold => 0.5f;

  public override bool AllowRestingInBed => false;

  public override void UpdateAllDuties()
  {
    foreach (Pawn pawn in lord.ownedPawns)
    {
      if (pawn.InVehicle())
        continue;

      pawn.mindState.duty = pawn is VehiclePawn ?
        new PawnDuty(DutyDefOf_Vehicles.WaitVehicle) :
        new PawnDuty(DutyDefOf_Vehicles.PrepareVehicleCaravan_BoardVehicle)
        {
          locomotion = LocomotionUrgency.Jog
        };
    }
  }

  public override void LordToilTick()
  {
    const int CheckInterval = 200;

    if (Find.TickManager.TicksGame % CheckInterval == 0)
    {
      for (int i = lord.ownedPawns.Count; --i >= 0;)
      {
        Pawn pawn = lord.ownedPawns[i];
        if (pawn.InVehicle())
          continue;

        LordJob_FormAndSendVehicles lordJob = (LordJob_FormAndSendVehicles)lord.LordJob;
        AssignedSeat assignedSeat = lordJob.GetAssignedSeat(pawn);
        if (assignedSeat?.handler is null)
          continue;

        if (!assignedSeat.Vehicle.AllPawnsAboard.Contains(pawn))
          return;
      }
      lord.ReceiveMemo(MemoTrigger.PawnsOnboard);
    }
  }
}