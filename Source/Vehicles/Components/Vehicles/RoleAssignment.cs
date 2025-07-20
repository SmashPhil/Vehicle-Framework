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

  public bool TryPull(VehicleRoleHandler handler, out Pawn pawn)
  {
    HandlingType handlingType = handler.role.HandlingTypes;

    pawn = null;
    if (handlingType.HasFlag(HandlingType.Movement))
    {
      if (!drivers.NullOrEmpty())
        pawn = drivers.Pop();
      // Pull from turret operators as a last resort
      if (!turretOperators.NullOrEmpty())
        pawn ??= turretOperators.Pop();
    }
    if (handlingType.HasFlag(HandlingType.Turret))
    {
      if (!turretOperators.NullOrEmpty())
        pawn ??= turretOperators.Pop();
    }
    if (!passengers.NullOrEmpty())
      pawn ??= passengers.Pop();
    return pawn != null;
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