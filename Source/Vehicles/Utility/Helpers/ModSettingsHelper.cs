using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles
{
  public static class ModSettingsHelper
  {
    private const int MediumMapSize = 250;

    public static float BeachMultiplier(float coastWidth, Map map)
    {
      const float MaxBeachSize = 60f;

      if (Mathf.Approximately(VehicleMod.settings.main.beachMultiplier, 0))
        return coastWidth;
      // % is based on medium sized map
      float mapSizeMultiplier =
        (float)(map.Size.x >= map.Size.z ? map.Size.x : map.Size.z) / MediumMapSize;
      // Set to max possible width by vanilla standards, then apply multiplier
      return MaxBeachSize * (1 + VehicleMod.settings.main.beachMultiplier) * mapSizeMultiplier;
    }

    public static float RiverMultiplier(RiverDef riverDef)
    {
      if (Mathf.Approximately(VehicleMod.settings.main.riverMultiplier, 0))
        return riverDef.widthOnMap;
      return riverDef.widthOnMap * (1 + VehicleMod.settings.main.riverMultiplier);
    }
  }
}