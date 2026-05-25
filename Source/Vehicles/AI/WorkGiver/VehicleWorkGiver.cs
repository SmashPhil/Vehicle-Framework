using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles;

[UsedWithReflection]
public abstract class VehicleWorkGiver : WorkGiver_Scanner
{
  public abstract JobDef JobDef { get; }

  public override PathEndMode PathEndMode => PathEndMode.Touch;

  public abstract bool CanBeWorkedOn(VehiclePawn vehicle);

  public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
  {
    if (t.Faction != pawn.Faction || t is not VehiclePawn vehicle)
      return null;

    if (!vehicle.vehiclePather.Moving || !CanBeWorkedOn(vehicle))
      return null;

    if (!pawn.CanReach(new LocalTargetInfo(t.Position), PathEndMode.Touch, Danger.Deadly))
      return null;

    VehicleReservationManager reservationManager = pawn.Map.GetCachedMapComponent<VehicleReservationManager>();
    if (reservationManager.CanReserve(vehicle, pawn, JobDef))
    {
      IntVec3 jobCell = vehicle.SurroundingCells.RandomOrFallback(cell =>
        reservationManager.CanReserve<LocalTargetInfo, VehicleTargetReservation>(vehicle, pawn, cell), IntVec3.Invalid);
      if (jobCell.IsValid)
      {
        return JobMaker.MakeJob(JobDef, vehicle, jobCell);
      }
    }

    return null;
  }
}