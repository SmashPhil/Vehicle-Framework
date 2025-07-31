using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Vehicles;

// TODO - can be reworked as a general role distributor
public class RoleAssignment
{
  private readonly List<Pawn> drivers = [];
  private readonly List<Pawn> turretOperators = [];
  private readonly List<Pawn> passengers = [];

  public bool IsEmpty =>
    drivers.Count == 0 && turretOperators.Count == 0 && passengers.Count == 0;

  public void Set(IEnumerable<Pawn> pawns)
  {
    drivers.Clear();
    turretOperators.Clear();
    passengers.Clear();

    foreach (Pawn pawn in pawns)
    {
      // TODO - needs faction check for prisoners

      // Add pawn to best candidate role, turrets take priority but drivers will pull first
      if (VehicleRoleHandler.CanOperateRole(pawn, HandlingType.Turret))
      {
        turretOperators.Add(pawn);
      }
      else if (VehicleRoleHandler.CanOperateRole(pawn, HandlingType.Movement))
      {
        drivers.Add(pawn);
      }
      else
      {
        passengers.Add(pawn);
      }
    }
    Resort();
  }

  // TODO - passengers could be prioritized based on move speed so the slowest are assigned to vehicles
  // first, giving optimal move speeds in vehicle caravans.
  public bool TryPull(VehicleRoleHandler handler, out Pawn pawn)
  {
    HandlingType handlingType = handler.role.HandlingTypes;

    pawn = null;
    if (handlingType.HasFlag(HandlingType.Turret))
    {
      pawn ??= TryPop(turretOperators);
    }
    else if (handlingType.HasFlag(HandlingType.Movement))
    {
      pawn ??= TryPop(drivers);
      // Pull from turret operators as a last resort, though most pawns will be turret operators
      // as long as they are capable of shooting / violence.
      pawn ??= TryPop(turretOperators);
    }
    else if (handlingType == HandlingType.None)
    {
      pawn ??= TryPop(turretOperators);
      pawn ??= TryPop(drivers);
      pawn ??= TryPop(passengers);
    }
    return pawn != null;

    static Pawn TryPop(List<Pawn> pawns)
    {
      return pawns.Count == 0 ? null : pawns.Pop();
    }
  }

  private void Resort()
  {
    const float ConsciousnessWeight = 2;
    // Consciousness takes priority, pawn will be less likely to down while driving.
    drivers.SortByDescending(pawn =>
      pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness) * ConsciousnessWeight +
      pawn.health.capacities.GetLevel(PawnCapacityDefOf.Consciousness));
    turretOperators.SortByDescending(pawn => pawn.skills.GetSkill(SkillDefOf.Shooting).Aptitude);
  }
}