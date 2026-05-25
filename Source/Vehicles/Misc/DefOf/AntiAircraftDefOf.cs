using RimWorld;

namespace Vehicles.World;

// ReSharper disable InconsistentNaming
[DefOf]
public static class AntiAircraftDefOf
{
  public static AntiAircraftDef FlakProjectile;

  static AntiAircraftDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(AntiAircraftDefOf));
  }
}