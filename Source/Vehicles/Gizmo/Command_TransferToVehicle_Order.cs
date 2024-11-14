using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Vehicles
{
	public class Command_TransferToVehicle_Order : Command_Target
	{
		public static Command_TransferToVehicle_Order Instance { get; }

		private Command_TransferToVehicle_Order()
		{
			defaultLabel = "VF_TransferToVehicle_Order".Translate();
			defaultDesc = "VF_TransferToVehicle_Order_Desc".Translate();
			icon = VehicleTex.PackCargoIcon[(uint)VehicleType.Land];
			Order = 1000f;
			action = Action;
			targetingParams = TargetingParameters.ForPawns();
			targetingParams.validator = IsVehicle;
		}

		static Command_TransferToVehicle_Order()
		{
			Instance = new Command_TransferToVehicle_Order();
		}

		private static bool IsVehicle(TargetInfo target)
		{
			return target.Thing is VehiclePawn;
		}

		public static IEnumerable<Thing> GetSelectedTransferableThings()
		{
			foreach (var obj in Find.Selector.SelectedObjects)
			{
				if (obj is Thing thing && thing.CanBeTransferredToVehiclesCargo())
				{
					yield return thing;
				}
			}
		}

		private static void Action(LocalTargetInfo target)
		{
			if (target.Thing is VehiclePawn vehicle)
			{
				GetSelectedTransferableThings().TransferToVehicle(vehicle);
			}
		}
	}
}
