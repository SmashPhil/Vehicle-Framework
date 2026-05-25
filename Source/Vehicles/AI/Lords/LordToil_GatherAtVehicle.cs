using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

public class LordToil_GatherAtVehicle : LordToil
{
  protected const int MeetingPointDist = 10;
  protected readonly IntVec3 meetingSpot;

  public LordToil_GatherAtVehicle(IntVec3 meetingSpot)
  {
    this.meetingSpot = meetingSpot;
  }

  public override float? CustomWakeThreshold => 0.5f;

  public override bool AllowRestingInBed => false;

  public override bool ShouldFail => !meetingSpot.IsValid;

  public override void UpdateAllDuties()
  {
    foreach (Pawn pawn in lord.ownedPawns)
    {
      if (pawn.InVehicle())
        continue;

      pawn.mindState.duty = pawn is VehiclePawn ?
        new PawnDuty(DutyDefOf_Vehicles.WaitVehicle) :
        new PawnDuty(DutyDefOf.TravelOrWait, meetingSpot)
        {
          locomotion = LocomotionUrgency.Jog
        };
    }
  }

  public override void LordToilTick()
  {
    const int CheckInterval = 120;

    if (Find.TickManager.TicksGame % CheckInterval != 0)
      return;

    for (int i = lord.ownedPawns.Count; --i >= 0;)
    {
      Pawn pawn = lord.ownedPawns[i];
      if (pawn.InVehicle() || pawn is VehiclePawn)
        continue;

      if (!pawn.Spawned || !pawn.Position.InHorDistOf(meetingSpot, MeetingPointDist))
        return;

      if (!pawn.CanReach(meetingSpot, PathEndMode.ClosestTouch, Danger.Deadly))
      {
        lord.RemovePawn(pawn);
      }
    }
    lord.ReceiveMemo(MemoTrigger.PawnsOnboard);
  }
}
