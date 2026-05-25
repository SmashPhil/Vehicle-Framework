using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

internal class LordToil_BoardVehicle(VehiclePawn vehicle) : LordToil
{
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
        new PawnDuty(DutyDefOf_Vehicles.BoardVehicle)
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

      if (pawn.GetVehicle() != vehicle)
        return;
    }
    lord.ReceiveMemo(MemoTrigger.PawnsOnboard);
  }
}
