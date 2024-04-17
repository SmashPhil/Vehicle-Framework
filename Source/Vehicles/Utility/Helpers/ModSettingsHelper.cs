using Verse;
using RimWorld;

namespace Vehicles
{
	public static class ModSettingsHelper
	{
		private const int MediumMapSize = 250;

		public static float BeachMultiplier(float coastWidth, Map map)
		{
			if (VehicleMod.settings.main.beachMultiplier <= 0)
			{
				return coastWidth; //If no beach multiplier, just return vanilla
			}
			float mapSizeMultiplier = (map.Size.x >= map.Size.z ? map.Size.x : map.Size.z) / MediumMapSize; //% is based on medium sized map at 250x250
			float beach = 60f; //Set to max possible width by vanilla standards, then apply multiplier
			return beach * (1 + VehicleMod.settings.main.beachMultiplier) * mapSizeMultiplier;
		}

		public static float RiverMultiplier(RiverDef riverDef)
		{
			return riverDef.widthOnMap * (1 + VehicleMod.settings.main.riverMultiplier);
		}
	}
}
