using Verse;

namespace Vehicles.Rendering;

public class Command_TargeterCooldownAction : Command_CooldownAction
{
  public override void FireTurrets()
  {
    FireTurret(turret);
    if (!turret.groupKey.NullOrEmpty())
    {
      Log.Warning("groupKey is not yet supported for Rotatable turrets.");
    }
  }

  public override void FireTurret(VehicleTurret turret)
  {
    if (turret.ReloadTicks <= 0)
    {
      HaltTurret();
      TurretTargeter.BeginTargeting(targetingParams, CommitTarget, turret);
    }
  }

  private void CommitTarget(LocalTargetInfo target)
  {
    turret.SetTarget(target);
    turret.ResetPrefireTimer();
  }
}
