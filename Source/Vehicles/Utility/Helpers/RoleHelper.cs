using System.Collections.Generic;
using System.Linq;
using SmashTools;
using UnityEngine.Assertions;
using Verse;

namespace Vehicles;

public static class RoleHelper
{
	// TODO - use RoleAssignment
	public static void Distribute(List<VehiclePawn> vehicles, List<Pawn> pawns)
	{
		pawns.RemoveAll(Ext_Vehicles.InVehicle);
		RotatingList<VehiclePawn> vehicleRotator = vehicles.ToRotatingList();
		DistributeOnPriority(vehicleRotator, pawns, HandlingType.Movement);
		DistributeOnPriority(vehicleRotator, pawns, HandlingType.Turret);
		DistributeAll(vehicleRotator, pawns);
	}

	private static void DistributeOnPriority(RotatingList<VehiclePawn> vehicles, List<Pawn> pawns,
		HandlingType handlingType)
	{
		foreach (VehiclePawn vehicle in vehicles)
		{
			foreach (VehicleRoleHandler roleHandler in vehicle.GetHandlers(handlingType).OrderBy(handler => handler))
			{
				if (pawns.Count == 0)
					return;
				if (roleHandler.RoleFulfilled || !roleHandler.CanOperateRole(pawns[^1]))
					continue;

				Pawn pawn = pawns.Pop();
				Assert.IsFalse(pawn.InVehicle());
				vehicle.TryAddPawn(pawn, roleHandler);
			}
		}
	}

	private static void DistributeAll(RotatingList<VehiclePawn> vehicles, List<Pawn> pawns)
	{
		while (pawns.Count > 0 && vehicles.Count > 0)
		{
			VehiclePawn vehicle = vehicles.Next;
			if (vehicle.handlers.Sum(handler => handler.role.Slots) == 0)
			{
				bool removed = vehicles.Remove(vehicle);
				Trace.IsTrue(removed, "Failed to remove vehicle from distribution.");
				continue;
			}
			Pawn pawn = pawns.Pop();
			Assert.IsFalse(pawn.InVehicle());
			vehicle.TryAddPawn(pawn);
		}
	}
}