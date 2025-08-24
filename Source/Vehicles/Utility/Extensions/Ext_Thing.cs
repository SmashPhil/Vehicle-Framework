using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles;

public static class Ext_Thing
{
	public static bool ShouldAlwaysTransferToVehiclesCargo(this Pawn pawn)
	{
		return pawn.IsAnimal || pawn.IsColonyMech;
	}

	public static bool CanBeTransferredToVehiclesCargo(this Pawn pawn)
	{
		return pawn.Downed || pawn.ShouldAlwaysTransferToVehiclesCargo();
	}

	public static bool CanBeHauledToVehicle(this Thing thing)
	{
		if (thing is Pawn pawn && pawn.Faction == Faction.OfPlayer && (pawn.Downed || pawn.IsAnimal))
			return true;

		if (!thing.Spawned)
			return false;

		if (thing.Map.IsPlayerHome && !thing.Map.areaManager.Home[thing.Position])
			return false;

		return thing.def.EverHaulable;
	}

	public static IEnumerable<VehiclePawn> GetVehiclesToBeTransferredTo(this Thing thing)
	{
		Map map = thing.MapHeld;

		if (map == null)
			yield break;

		IEnumerable<VehiclePawn> vehicles = map.GetCachedMapComponent<VehicleReservationManager>()
		 .VehicleListers(ReservationType.LoadVehicle);

		foreach (VehiclePawn vehicle in vehicles)
		{
			if (vehicle.cargoToLoad?.FindTransferableFor(thing) != null)
			{
				yield return vehicle;
			}
		}
	}

	public static bool IsOrderedToBeTransferredToAnyVehicle(this Thing thing)
	{
		return thing.GetVehiclesToBeTransferredTo().Any();
	}

	public static void TransferToVehicle(this IEnumerable<Thing> things, VehiclePawn vehicle)
	{
		foreach (Thing thing in things)
		{
			thing.CancelTransferToAnyOtherVehicle(vehicle);
			thing.TransferToVehicle(vehicle);
		}

		vehicle.MapHeld?.GetCachedMapComponent<VehicleReservationManager>()
		 .RegisterLister(vehicle, ReservationType.LoadVehicle);
	}

	public static void TransferToVehicle(this Thing thing, VehiclePawn vehicle)
	{
		(vehicle.cargoToLoad ??= []).AddThing(thing);
	}

	public static void CancelTransferToVehicle(this Thing thing, VehiclePawn vehicle)
	{
		if (vehicle.cargoToLoad?.RemoveThing(thing) == true)
		{
			thing.CancelRelatedJob(JobDefOf_Vehicles.LoadVehicle, JobCondition.QueuedNoLongerValid);
		}
	}

	public static void CancelTransferToAnyOtherVehicle(this Thing thing, VehiclePawn vehicle)
	{
		foreach (VehiclePawn otherVehicle in thing.GetVehiclesToBeTransferredTo())
		{
			if (otherVehicle != vehicle)
			{
				thing.CancelTransferToVehicle(otherVehicle);
			}
		}
	}

	public static void CancelTransferToAnyVehicle(this Thing thing)
	{
		foreach (VehiclePawn vehicle in thing.GetVehiclesToBeTransferredTo())
		{
			thing.CancelTransferToVehicle(vehicle);
		}
	}

	public static void CancelRelatedJob(this Thing thing, JobDef jobType,
		JobCondition cancelCondition)
	{
		ReservationManager.Reservation reservation =
			thing.MapHeld?.reservationManager.ReservationsReadOnly.FirstOrFallback(res =>
				res?.Target.Thing == thing);

		if (reservation != null && reservation.Job.def == jobType)
		{
			reservation.Job.GetCachedDriver(reservation.Claimant).EndJobWith(cancelCondition);
		}
	}
}