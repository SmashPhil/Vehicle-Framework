using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public static class Ext_Thing
{
  // TODO 1.7 - Move to RoleHelper
  extension(Pawn pawn)
  {
    public bool ShouldAlwaysTransferToVehiclesCargo()
    {
      return pawn.IsAnimal || pawn.IsColonyMech;
    }

    // TODO 1.7 - Remove
    // Placing pawns in cargo used to allow downed pawns, but this has resulted in unexpected behavior for players
    // expecting the pawn to be inside a passenger role. They should instead be placed in the least impactful role
    // possible, e.g. HandlingTypes None > Turret, with Movement being disallowed entirely.
    [Obsolete("Use ShouldAlwaysTransferToVehiclesCargo instead.")]
    public bool CanBeTransferredToVehiclesCargo()
    {
      return pawn.ShouldAlwaysTransferToVehiclesCargo();
    }
  }
  
  public static bool CanBeCarriedToVehicle(Pawn pawn)
  {
    if (!pawn.Spawned)
      return false;

    if (pawn.IsPrisonerOfColony)
      return pawn.guest.PrisonerIsSecure;

    return pawn.AnimalOrWildMan() || pawn.Downed;
  }

  public static bool CanBeHauledToVehicle(this Thing thing)
  {
    if (!thing.Spawned)
      return false;

    if (thing is Pawn pawn)
    {
      if (pawn.Faction == Faction.OfPlayer && (pawn.Downed || pawn.AnimalOrWildMan() || pawn.IsColonyMech))
        return true;

      if (pawn.IsPrisonerOfColony)
        return pawn.guest.PrisonerIsSecure;
    }

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