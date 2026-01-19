using RimWorld;
using Verse;

namespace Vehicles;

// ReSharper disable InconsistentNaming
[DefOf]
public static class SoundDefOf_Vehicles
{
	// AerialVehicles
	public static SoundDef AerialVehicle_Paratroopers_FlyOver;

	// Misc
	public static SoundDef Explode_BombWater;
	//public static SoundDef VF_ApplyingPaint;

  public static SoundDef TireScreech;

  static SoundDefOf_Vehicles()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(SoundDefOf_Vehicles));
	}
}