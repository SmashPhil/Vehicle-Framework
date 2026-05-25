using System.Linq;
using SmashTools;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

public class LordToil_PrepareCaravan_LeaveWithVehicles : LordToil, IDebugLordMeetingPoint
{
  private readonly IntVec3 exitSpot;

  public LordToil_PrepareCaravan_LeaveWithVehicles(IntVec3 exitSpot)
  {
    this.exitSpot = exitSpot;
  }

  public IntVec3 MeetingPoint => exitSpot;

  public override bool AllowSatisfyLongNeeds => false;

  public override float? CustomWakeThreshold => 0.5f;

  public override bool AllowRestingInBed => false;

  public override bool AllowSelfTend => false;

  public override void Init()
  {
    base.Init();
    foreach (Pawn pawn in lord.ownedPawns)
    {
      pawn.roping?.BreakAllRopes();
    }
  }

  public override void UpdateAllDuties()
  {
    RotatingList<VehiclePawn> vehicles = lord.ownedPawns.Where(p => p is VehiclePawn)
     .Cast<VehiclePawn>().ToRotatingList();
    foreach (Pawn pawn in lord.ownedPawns)
    {
      if (pawn.InVehicle())
        continue;

      if (pawn is VehiclePawn vehicle)
      {
        vehicle.ignition.Drafted = true;
        pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.TravelOrWaitVehicle, exitSpot)
        {
          locomotion = LocomotionUrgency.Jog
        };
        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
      }
      else
      {
        VehiclePawn nextVehicle = vehicles.Next;
        pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.FollowVehicle, nextVehicle,
          nextVehicle.VehicleDef.Size.z * 1.5f)
        {
          locomotion = LocomotionUrgency.Sprint
        };
      }
    }
  }

  public override void LordToilTick()
  {
    if (Find.TickManager.TicksGame % 100 == 0)
    {
      ExitMapUtility.CheckArrived(lord, lord.ownedPawns, exitSpot, MemoTrigger.ExitMap,
        CheckPawnArrived, PawnCanMove);
    }
    return;

    static bool CheckPawnArrived(Pawn pawn)
    {
      return !pawn.InVehicle();
    }

    static bool PawnCanMove(Pawn pawn)
    {
      return pawn is not VehiclePawn vehicle || vehicle.CanMoveFinal;
    }
  }
}