using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

public class JobGiver_SabotageVehicle : ThinkNode_JobGiver
{
  private float healthPct = 0.35f;
  private float maxDistance = 10;

  public override ThinkNode DeepCopy(bool resolve = true)
  {
    JobGiver_SabotageVehicle jobGiver = (JobGiver_SabotageVehicle)base.DeepCopy(resolve);
    jobGiver.maxDistance = maxDistance;
    jobGiver.healthPct = healthPct;
    return base.DeepCopy(resolve);
  }

  protected override Job TryGiveJob(Pawn pawn)
  {
    if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving))
      return null;
    if (!pawn.TryGetLord(out Lord lord))
      return null;

    foreach (Pawn ownedPawn in lord.ownedPawns)
    {
      // NOTE - Need to use <= operator here for AttachedExplosives boolean condition, only
      // relational comparisons are allowed for property patterns.
      if (ownedPawn is VehiclePawn { CanMove: false, AttachedExplosives: <= 0 } vehicle &&
        vehicle.statHandler.HealthPercent > healthPct &&
        pawn.Position.InHorDistOf(vehicle.Position, maxDistance))
      {
        VehicleReservationManager
          resMgr = pawn.Map.GetCachedMapComponent<VehicleReservationManager>();
        if (resMgr.CanReserve(vehicle, pawn, JobDefOf_Vehicles.SabotageVehicle))
        {
          IntVec3 jobCell = vehicle.SurroundingCells.RandomOrFallback(cell =>
              resMgr.CanReserve<LocalTargetInfo, VehicleTargetReservation>(vehicle, pawn, cell),
            IntVec3.Invalid);
          return JobMaker.MakeJob(JobDefOf_Vehicles.SabotageVehicle, vehicle, jobCell);
        }
      }
    }

    return null;
  }
}