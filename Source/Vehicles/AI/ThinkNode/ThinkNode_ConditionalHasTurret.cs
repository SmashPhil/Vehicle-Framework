using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class ThinkNode_ConditionalHasTurret : ThinkNode_Conditional
{
  protected override bool Satisfied(Pawn pawn)
  {
    VehiclePawn vehicle = pawn as VehiclePawn;
    // Should never reach this conditional if pawn is not a vehicle
    Assert.IsNotNull(vehicle);
    return vehicle.CompVehicleTurrets != null && vehicle.CompVehicleTurrets.Turrets.Count > 0;
  }
}