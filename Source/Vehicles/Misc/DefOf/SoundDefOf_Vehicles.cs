using RimWorld;
using Verse;

namespace Vehicles
{
	[DefOf]
	public static class SoundDefOf_Vehicles
	{
		static SoundDefOf_Vehicles()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(SoundDefOf_Vehicles));
		}

		public static SoundDef Explode_BombWater;

		//public static SoundDef VF_ApplyingPaint;
	}
}
