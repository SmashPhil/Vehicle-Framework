using RimWorld;

namespace Vehicles;

// ReSharper disable InconsistentNaming
[DefOf]
public static class PatternDefOf
{
	public static PatternDef Default;

	static PatternDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(PatternDefOf));
	}
}