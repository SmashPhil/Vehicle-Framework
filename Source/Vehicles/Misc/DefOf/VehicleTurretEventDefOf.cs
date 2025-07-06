using RimWorld;

namespace Vehicles;

// ReSharper disable InconsistentNaming
[DefOf]
public static class VehicleTurretEventDefOf
{
  static VehicleTurretEventDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(VehicleTurretEventDefOf));
  }

  public static VehicleTurretEventDef Queued;
  public static VehicleTurretEventDef Dequeued;
  public static VehicleTurretEventDef ShotFired;
  public static VehicleTurretEventDef Reload;
  public static VehicleTurretEventDef Warmup;
  public static VehicleTurretEventDef Cooldown;
}