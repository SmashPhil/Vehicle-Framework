using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles
{
  public class JobGiver_RangedSupport : JobGiver_CombatFormation
  {
    protected override IntRange ExpiryInterval => new(120, 240);

    protected override bool TryFindCombatPosition(VehiclePawn vehicle, out IntVec3 dest)
    {
      return CombatPositionFinder.TryFindShootPosition(
        CombatPositionFinder.Request.For(vehicle, vehicle.mindState.enemyTarget), out dest);
    }

    protected override void UpdateEnemyTarget(VehiclePawn vehicle)
    {
      const TargetScanFlags ScanFlags = TargetScanFlags.NeedLOSToPawns |
        TargetScanFlags.NeedReachableIfCantHitFromMyPos | TargetScanFlags.NeedThreat |
        TargetScanFlags.NeedAutoTargetable;

      Thing thing = vehicle.mindState.enemyTarget;
      if (thing != null && ShouldLoseTarget(vehicle))
      {
        thing = null;
      }
      if (thing == null)
      {
        thing = CombatTargetFinder.FindAttackTarget(vehicle, ScanFlags,
          validator: (target) => ExtraTargetValidator(vehicle, target),
          minDistance: vehicle.CompVehicleTurrets.MinRange,
          maxDistance: vehicle.CompVehicleTurrets.MaxRange, onlyRanged: true);
        if (thing != null)
        {
          Notify_EngagedTarget(vehicle.mindState);
          Lord lord = vehicle.GetLord();
          lord?.Notify_PawnAcquiredTarget(vehicle, thing);
        }
      }
      else
      {
        //Thing thing2 = CombatTargetFinder.FindAttackTarget(vehicle, scanFlags, validator: (Thing target) => ExtraTargetValidator(vehicle, target),
        //	minDistance: vehicle.CompVehicleTurrets.MinRange, maxDistance: vehicle.CompVehicleTurrets.MaxRange, onlyRanged: true);
        //if (thing2 == null && !vehicle.VehicleDef.npcProperties.runDownTargets)
        //{
        //	thing = null;
        //}
        //else if (thing2 != null && thing2 != thing)
        //{
        //	Notify_EngagedTarget(vehicle.mindState);
        //	thing = thing2;
        //}
      }
      vehicle.mindState.enemyTarget = thing;
      if (thing is Pawn && thing.Faction == Faction.OfPlayer &&
        vehicle.Position.InHorDistOf(thing.Position, 60f))
      {
        Find.TickManager.slower.SignalForceNormalSpeed();
      }
    }
  }
}