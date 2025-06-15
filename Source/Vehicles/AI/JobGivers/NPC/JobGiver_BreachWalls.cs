using System.Linq;
using System.Threading;
using RimWorld;
using SmashTools;
using SmashTools.Performance;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles
{
  [NoProfiling]
  public class JobGiver_BreachWalls : JobGiver_RangedSupport
  {
    protected override Job TryGiveJob(Pawn pawn)
    {
      const int DistanceThreshold = 25;
      const int RegionLookCount = 9;

      VehiclePawn vehicle = pawn as VehiclePawn;
      Assert.IsNotNull(vehicle);
      VehicleDef vehicleDef = vehicle.VehicleDef;
      IntVec3 cell = vehicle.mindState.duty.focus.Cell;
      if (cell.IsValid && cell.DistanceToSquared(vehicle.Position) < DistanceThreshold &&
        VehicleRegionAndRoomQuery.RoomAtFast(cell, vehicle.Map, vehicleDef) ==
        VehicleRegionAndRoomQuery.RoomAtFast(vehicle.Position, vehicle.Map, vehicleDef) &&
        cell.WithinRegions(vehicle.Position, vehicle.Map, vehicleDef, RegionLookCount,
          TraverseMode.NoPassClosedDoors))
      {
        vehicle.GetLord().Notify_ReachedDutyLocation(vehicle);
        return null;
      }
      if (!cell.IsValid)
      {
        // If there's no valid target to attack for breach job and destination is invalid, let
        // think tree fall through and assign normal raid duties.
        // TODO - align with CompTargetFinder so target acquisition is the same
        if (!pawn.Map.attackTargetsCache.GetPotentialTargetsFor(pawn).Where(target =>
            !target.ThreatDisabled(vehicle) && target.Thing.Faction == Faction.OfPlayer
            && vehicle.CanReachVehicle(target.Thing.Position, PathEndMode.OnCell, Danger.Deadly,
              mode: TraverseMode.PassAllDestroyableThings))
         .TryRandomElement(out IAttackTarget attackTarget))
        {
          return null;
        }
        cell = attackTarget.Thing.Position;
      }
      if (!vehicle.CanReachVehicle(cell, PathEndMode.OnCell, Danger.Deadly,
        TraverseMode.PassAllDestroyableThings))
      {
        return null;
      }
      VehiclePathingSystem mapping = MapComponentCache<VehiclePathingSystem>.GetComponent(vehicle.Map);
      using (VehiclePath vehiclePath = mapping[vehicleDef].VehiclePathFinder.FindPath(
        vehicle.Position, cell,
        TraverseParms.For(vehicle, mode: TraverseMode.PassAllDestroyableThings),
        CancellationToken.None))
      {
        Thing thing =
          PathingHelper.FirstBlockingBuilding(vehicle, vehiclePath);
        if (thing != null && TryFindCombatPosition(vehicle, out IntVec3 firingPos))
        {
          vehicle.mindState.breachingTarget = new BreachingTargetData(thing, firingPos);
          cell = firingPos;
        }
      }
      return JobMaker.MakeJob(JobDefOf.Goto, cell, 500, true);
    }
  }
}