using SmashTools;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

public class LordToil_TravelToDestination : LordToil
{
  protected readonly VehiclePawn vehicle;
  protected readonly IntVec3 destination;
  protected readonly Rot8? endRot;

  public LordToil_TravelToDestination(VehiclePawn vehicle, IntVec3 destination, Rot8? endRot = null)
  {
    this.vehicle = vehicle;
    this.destination = destination;
    this.endRot = endRot;
  }

  public override bool ShouldFail
  {
    get
    {
      if (!vehicle.CanMove)
        return true;

      if (!vehicle.CanReachVehicle(destination, PathEndMode.OnCell, Danger.Deadly))
        return true;

      return false;
    }
  }

  public override void Init()
  {
    vehicle.ignition.Drafted = true;
  }

  public override void Cleanup()
  {
    vehicle.ignition.Drafted = false;
    if (endRot != null)
    {
      vehicle.FullRotation = endRot.Value;
    }
  }

  public override void UpdateAllDuties()
  {
    Assert.IsTrue(lord.ownedPawns.Contains(vehicle));
    for (int i = lord.ownedPawns.Count; --i >= 0;)
    {
      Pawn pawn = lord.ownedPawns[i];
      if (pawn is not VehiclePawn)
        continue;

      pawn.mindState.duty = new PawnDuty(DutyDefOf_Vehicles.TravelOrWaitVehicle, destination);
    }
  }

  protected virtual void ArrivedAtDest()
  {
    lord.ReceiveMemo(MemoTrigger.ArrivedAtDestination);
  }

  public override void LordToilTick()
  {
    if (vehicle.Position == destination)
    {
      ArrivedAtDest();
    }
  }
}
