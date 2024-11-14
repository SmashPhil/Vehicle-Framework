using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles
{
	public static class Ext_Thing
	{
		public static bool CanBeTransferredToVehiclesCargo(this Thing thing)
		{
			return
				thing.def.EverHaulable
				||
				(
					thing is Pawn pawn
					&&
					pawn is VehiclePawn == false
					&&
					(
						pawn.Faction?.IsPlayer == true
						||
						pawn.Downed
					)
				);
		}

		public static IEnumerable<VehiclePawn> GetVehiclesToBeTransferredTo(this Thing thing)
		{
			var map = thing.MapHeld;

			if (map == null)
			{
				yield break;
			}

			var vehicles = map.GetCachedMapComponent<VehicleReservationManager>().VehicleListers(ReservationType.LoadVehicle);

			foreach (var vehicle in vehicles)
			{
				if (vehicle.cargoToLoad?.FindTransferableFor(thing) != null)
				{
					yield return vehicle;
				}
			}
		}

		public static bool IsOrderedToBeTransferredToAnyVehicle(this Thing thing)
		{
			return thing.GetVehiclesToBeTransferredTo().Count() > 0;
		}

		public static void TransferToVehicle(this IEnumerable<Thing> things, VehiclePawn vehicle)
		{
			foreach (var thing in things)
			{
				thing.CancelTransferToAnyOtherVehicle(vehicle);
				thing.TransferToVehicle(vehicle);
			}

			vehicle.MapHeld?.GetCachedMapComponent<VehicleReservationManager>().RegisterLister(vehicle, ReservationType.LoadVehicle);
		}

		public static void TransferToVehicle(this Thing thing, VehiclePawn vehicle)
		{
			(vehicle.cargoToLoad ??= new List<TransferableOneWay>()).AddThing(thing);
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
			foreach (var otherVehicle in thing.GetVehiclesToBeTransferredTo())
			{
				if (otherVehicle != vehicle)
				{
					thing.CancelTransferToVehicle(otherVehicle);
				}
			}
		}

		public static void CancelTransferToAnyVehicle(this Thing thing)
		{
			foreach (var vehicle in thing.GetVehiclesToBeTransferredTo())
			{
				thing.CancelTransferToVehicle(vehicle);
			}
		}

		public static void CancelRelatedJob(this Thing thing, JobDef jobType, JobCondition cancelCondition)
		{
			var reservation = thing.MapHeld?.reservationManager.ReservationsReadOnly.FirstOrFallback(res => res?.Target.Thing == thing);

			if (reservation?.Job.def == jobType)
			{
				reservation.Job.GetCachedDriver(reservation.Claimant).EndJobWith(cancelCondition);
			}
		}
	}
}
