using Verse;
using RimWorld;

namespace Vehicles
{
	public static class ModSettingsHelper
	{
		private const int MediumMapSize = 250;

		public static float BeachMultiplier(Map map)
		{
			float mapSizeMultiplier = (map.Size.x >= map.Size.z ? map.Size.x : map.Size.z) / MediumMapSize;
			float beach = 60f;
			if (VehicleMod.settings.main.beachMultiplier <= 0)
			{
				beach = Rand.Range(60f, 100f);
			}
			return (float)(beach + (beach * VehicleMod.settings.main.beachMultiplier)) * mapSizeMultiplier;
		}

		public static float RiverMultiplier(RiverDef riverDef)
		{
			return riverDef.widthOnMap * (1 + VehicleMod.settings.main.riverMultiplier);
		}
	}
}
