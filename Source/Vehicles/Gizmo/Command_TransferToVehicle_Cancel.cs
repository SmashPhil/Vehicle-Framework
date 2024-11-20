using Verse;

namespace Vehicles
{
	public class Command_TransferToVehicle_Cancel : Command_Action
	{
		public static Command_TransferToVehicle_Cancel Instance { get; }

		private Command_TransferToVehicle_Cancel()
		{
			defaultLabel = "VF_TransferToVehicle_Cancel".Translate();
			defaultDesc = "VF_TransferToVehicle_Cancel_Desc".Translate();
			icon = VehicleTex.CancelPackCargoIcon[(uint)VehicleType.Land];
			Order = 1001f;
			action = Action;
		}

		static Command_TransferToVehicle_Cancel()
		{
			Instance = new Command_TransferToVehicle_Cancel();
		}

		private static void Action()
		{
			foreach (var thing in Command_TransferToVehicle_Order.GetSelectedTransferableThings())
			{
				thing.CancelTransferToAnyVehicle();
			}
		}
	}
}
