using JetBrains.Annotations;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class ThinkNode_ConditionalVehicleState : ThinkNode_Conditional
{
  private bool? canMove;
  private bool? canTakeoff;
  private bool? hasPassengers;
  
  public override ThinkNode DeepCopy(bool resolve = true)
  {
    ThinkNode_ConditionalVehicleState thinkNode =
      (ThinkNode_ConditionalVehicleState)base.DeepCopy(resolve);
    thinkNode.canMove = canMove;
    thinkNode.canTakeoff = canTakeoff;
    thinkNode.hasPassengers = hasPassengers;

    return thinkNode;
  }

  protected override bool Satisfied(Pawn pawn)
  {
    if (pawn is not VehiclePawn vehicle)
    {
      return false;
    }

    if (canTakeoff.HasValue && vehicle.CompVehicleLauncher != null &&
      vehicle.CompVehicleLauncher.CanLaunchWithCargoCapacity(out string _) != canTakeoff.Value)
    {
      return false;
    }

    if (hasPassengers.HasValue && (vehicle.AllPawnsAboard.Count > 0) != hasPassengers.Value)
    {
      return false;
    }

    return !canMove.HasValue || vehicle.CanMove == canMove.Value;
  }
}