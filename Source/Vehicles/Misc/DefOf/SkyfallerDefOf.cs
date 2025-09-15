using RimWorld;
using Verse;

namespace Vehicles;

// ReSharper disable InconsistentNaming
[DefOf]
public static class SkyfallerDefOf
{
	// Paratroopers
	public static AirdropDef AirdropParatrooper;
	public static AirdropDef AirdropPackage;

	// Misc
	public static ThingDef ProjectileSkyfaller;

	static SkyfallerDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(SkyfallerDefOf));
	}
}