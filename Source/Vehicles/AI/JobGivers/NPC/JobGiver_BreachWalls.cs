using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;


namespace Vehicles;

[PublicAPI]
public class JobGiver_BreachWalls : JobGiver_RangedSupport
{
  protected override IntRange ExpiryInterval => new(240, 360);

  private static bool ReachedLocation(VehiclePawn vehicle, IntVec3 cell)
  {
    const int DistanceThreshold = 25;
    const int RegionLookCount = 9;

    if (!cell.IsValid)
      return false;

    if (cell.DistanceToSquared(vehicle.Position) >= DistanceThreshold)
      return false;

    VehicleDef vehicleDef = vehicle.VehicleDef;
    VehicleRoom curRoom = VehicleRegionAndRoomQuery.RoomAtFast(vehicle.Position, vehicle.Map, vehicleDef);
    VehicleRoom destRoom = VehicleRegionAndRoomQuery.RoomAtFast(cell, vehicle.Map, vehicleDef);
    if (destRoom != curRoom)
      return false;

    return cell.WithinRegions(vehicle.Position, vehicle.Map, vehicleDef, RegionLookCount, vehicle.DefaultTraverseMode());
  }

  protected override Job TryGiveJob(Pawn pawn)
  {
    VehiclePawn vehicle = pawn as VehiclePawn;
    IntVec3 cell = vehicle!.mindState.duty.focus.Cell;
    if (ReachedLocation(vehicle, cell))
    {
      vehicle.GetLord().Notify_ReachedDutyLocation(vehicle);
      return null;
    }
    if (!cell.IsValid)
    {
      // If there's no valid target to attack for breach job and destination is invalid, let
      // think tree fall through and assign normal raid duties.
      // TODO - align with CompTargetFinder so target acquisition is the same
      if (!pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn).Where(IsValidAttackTarget)
       .TryRandomElement(out IAttackTarget attackTarget))
      {
        return null;
      }
      cell = attackTarget.Thing.Position;

      bool IsValidAttackTarget(IAttackTarget potentialTarget)
      {
        if (potentialTarget.ThreatDisabled(vehicle))
          return false;

        if (vehicle.Faction != null && !vehicle.Faction.HostileTo(potentialTarget.Thing.Faction))
          return false;

        return vehicle.CanReachVehicle(potentialTarget.Thing.Position, PathEndMode.Touch, Danger.Deadly,
          mode: TraverseMode.PassAllDestroyableThings);
      }
    }
    else if (!vehicle.CanReachVehicle(cell, PathEndMode.OnCell, Danger.Deadly,
               TraverseMode.PassAllDestroyableThings))
    {
      return null;
    }

    if (CombatPositionFinder.TryFindBreachTarget(vehicle, cell, out Thing target, out IntVec3 firingPos))
    {
      vehicle.mindState.breachingTarget = new BreachingTargetData(target, firingPos);
    }
    return JobMaker.MakeJob(JobDefOf.Goto, cell, expiryInterval: ExpiryInterval.RandomInRange, true);
  }
}