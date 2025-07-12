using Verse;

namespace Vehicles;

public class Command_TransferToVehicle_Cancel : Command_Action
{
  public static readonly Command_TransferToVehicle_Cancel Command = new()
  {
    Order = 1001f
  };

  private Command_TransferToVehicle_Cancel()
  {
    defaultLabel = "VF_TransferToVehicle_Cancel".Translate();
    defaultDesc = "VF_TransferToVehicle_Cancel_Desc".Translate();
    icon = VehicleTex.CancelPackCargoIcon[(uint)VehicleType.Land];
    action = Action;
  }

  private static void Action()
  {
    foreach (Thing thing in Command_TransferToVehicle_Order.GetSelectedTransferableThings())
    {
      thing.CancelTransferToAnyVehicle();
    }
  }
}