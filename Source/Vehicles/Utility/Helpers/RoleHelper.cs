using System.Collections.Generic;
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
		for (int i = pawns.Count - 1; i >= 0; i--)
		{
			Pawn pawn = pawns[i];
			VehicleRoleHandler handler = GetAvailableHandler(pawn, vehicles, handlingType);
			if (handler != null && handler.vehicle.TryAddPawn(pawn, handler))
			{
				pawns.RemoveAt(i);
			}
		}
		return;

		static VehicleRoleHandler GetAvailableHandler(Pawn pawn, List<VehiclePawn> vehicles, HandlingType handlingType)
		{
			foreach (VehiclePawn vehicle in vehicles)
			{
				VehicleRoleHandler handler = vehicle.GetNextAvailableHandler(pawn, handlingType);
				if (handler != null)
					return handler;
			}
			return null;
		}
	}

	private static void DistributeAll(RotatingList<VehiclePawn> vehicles, List<Pawn> pawns)
	{
		int pawnIndex = pawns.Count - 1;
		while (pawns.Count > 0 && pawnIndex >= 0 && vehicles.Count > 0)
		{
			VehiclePawn vehicle = vehicles.Next;
			if (NoRemainingSeats(vehicle))
			{
				bool removed = vehicles.Remove(vehicle);
				Trace.IsTrue(removed, "Failed to remove vehicle from distribution.");
				continue;
			}
			Pawn pawn = pawns[pawnIndex];
			Assert.IsFalse(pawn.InVehicle());
			if (!vehicle.TryAddPawn(pawn))
			{
				Log.Error($"Unable to add {pawn} to vehicle {vehicle} during final distribution.");
			}
			pawnIndex--;
		}
		return;

		static bool NoRemainingSeats(VehiclePawn vehicle)
		{
			foreach (VehicleRoleHandler handler in vehicle.handlers)
			{
				if (handler.AreSlotsAvailable)
					return false;
			}
			return true;
		}
	}
}