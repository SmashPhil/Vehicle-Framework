using System.Collections.Generic;
using JetBrains.Annotations;
using Verse;
using Verse.AI;
using RimWorld;
using SmashTools;

namespace Vehicles;

public class WorkGiver_RemoveFuelFromVehicle : WorkGiver_Scanner
{
  public override PathEndMode PathEndMode => PathEndMode.Touch;

  public virtual JobDef JobStandard => JobDefOf_Vehicles.RemoveFuelFromVehicle;

  public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
  {
    VehicleReservationManager resManager = pawn.Map
     .GetCachedMapComponent<VehicleReservationManager>();
    return resManager.VehicleListers(ReservationType.RemoveFuel);
  }

  public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
  {
    return t is VehiclePawn { CompFueledTravel: not null, vehiclePather.Moving: false } vehicle &&
      CanRemoveFuel(pawn, vehicle, forced);
  }

  public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
  {
    if (t is VehiclePawn { CompFueledTravel: not null, vehiclePather.Moving: false } vehicle)
    {
      return JobMaker.MakeJob(JobDefOf_Vehicles.RemoveFuelFromVehicle, vehicle);
    }
    return null;
  }

  [PublicAPI]
  public static bool CanRemoveFuel(Pawn pawn, VehiclePawn vehicle, bool forced = false)
  {
    CompFueledTravel compFueler = vehicle.CompFueledTravel;
    if (compFueler is null)
      return false;

    if (vehicle.Faction != pawn.Faction)
      return false;

    if (!compFueler.CanEjectFuel)
      return false;

    if (vehicle.IsForbidden(pawn) || !pawn.CanReserve(vehicle, ignoreOtherReservations: forced))
      return false;

    return true;
  }
}