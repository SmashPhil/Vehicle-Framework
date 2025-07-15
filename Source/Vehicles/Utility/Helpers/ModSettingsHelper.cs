using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles;

public static class ModSettingsHelper
{
  public static FloatRange BeachMultiplier(FloatRange coastOffset)
  {
    float multiplier = VehicleMod.settings.main.beachMultiplier;
    if (Mathf.Approximately(multiplier, 0))
      return coastOffset;
    multiplier += 1; // Min is 100%, the multiplier is additive
    return new FloatRange(coastOffset.min * multiplier, coastOffset.max * multiplier);
  }

  public static float RiverMultiplier => 1 +
    (Mathf.Approximately(VehicleMod.settings.main.riverMultiplier, 0) ?
      0 :
      VehicleMod.settings.main.riverMultiplier);

  public static float RiverSizeWithMultiplier(RiverDef riverDef)
  {
    return riverDef.widthOnMap * RiverMultiplier;
  }
}