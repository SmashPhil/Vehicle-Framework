using RimWorld;

namespace Vehicles.World;

[DefOf]
public static class AntiAircraftDefOf
{
  static AntiAircraftDefOf()
  {
    DefOfHelper.EnsureInitializedInCtor(typeof(AntiAircraftDefOf));
  }

  public static AntiAircraftDef FlakProjectile;
}