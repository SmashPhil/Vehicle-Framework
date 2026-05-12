using JetBrains.Annotations;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class ThinkNode_ConditionalEngaging : ThinkNode_Conditional
{
  protected override bool Satisfied(Pawn pawn)
  {
    VehiclePawn vehicle = pawn as VehiclePawn;
    if (vehicle!.CompVehicleTurrets == null)
      return false;

    foreach (VehicleTurret turret in vehicle.CompVehicleTurrets.Turrets)
    {
      if (turret.targetInfo.IsValid)
        return true;
    }
    return false;
  }
}