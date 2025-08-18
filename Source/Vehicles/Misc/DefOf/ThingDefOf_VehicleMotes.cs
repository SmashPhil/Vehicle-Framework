using RimWorld;
using Verse;

namespace Vehicles;

// ReSharper disable InconsistentNaming
[DefOf]
public static class ThingDefOf_VehicleMotes
{
	static ThingDefOf_VehicleMotes()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(ThingDefOf_VehicleMotes));
	}

	public static ThingDef MoteFishingNet;

	public static ThingDef MoteLaunchedTurret;
}