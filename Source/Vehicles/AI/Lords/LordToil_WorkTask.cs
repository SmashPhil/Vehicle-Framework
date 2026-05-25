using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

internal class LordToil_WorkTask(IVehicleTask task) : LordToil
{
  public override float? CustomWakeThreshold => 0.5f;

  public override bool AllowRestingInBed => true;

  public override bool ShouldFail => task.ShouldFail;

  public override void Init()
  {
    task.StartTask();
  }

  public override void Cleanup()
  {
    task.FinishTask();
  }

  public override void UpdateAllDuties()
  {
    foreach (Pawn pawn in lord.ownedPawns)
    {
      if (pawn is not VehiclePawn)
      {
        // TODO - allow disembarking so pawns can work their job separate from the vehicle
        Assert.IsTrue(pawn.InVehicle());
        continue;
      }

      pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.WaitVehicle);
    }
  }

  public override void LordToilTick()
  {
    task.TaskTick();

    if (task.ShouldEnd)
    {
      lord.ReceiveMemo(MemoTrigger.TaskComplete);
    }
  }
}
