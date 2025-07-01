using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_RimHUD : ConditionalVehiclePatch
{
  public override string PackageId => ModPackageIds.RimHUD;

  public override void PatchAll(ModMetaData mod, Harmony harmony)
  {
    Type inspectPaneUtilityType =
      AccessTools.TypeByName("RimHUD.Access.Patch.RimWorld_InspectPaneUtility_InspectPaneOnGUI");
    harmony.Patch(AccessTools.Method(inspectPaneUtilityType, "Prefix"),
      prefix: new HarmonyMethod(typeof(Compatibility_RimHUD),
        nameof(DontRenderRimHUDForVehicles_InspectPaneUtility)));

    //Type inspectPaneFillerType = AccessTools.TypeByName("RimHUD.Patch.RimWorld_InspectPaneFiller_DoPaneContentsFor");
    //harmony.Patch(AccessTools.Method(inspectPaneFillerType, "Prefix"),
    //	prefix: new HarmonyMethod(typeof(Compatibility_RimHUD),
    //	nameof(DontRenderRimHUDForVehicles_InspectPaneFiller)));
  }

  private static bool DontRenderRimHUDForVehicles_InspectPaneUtility(ref bool __result)
  {
    // Null check on UIRoot is necessary since inspect pane can be called during starting map
    // location before UIRoot_Play loads
    if (Find.UIRoot is UIRoot_Play { mapUI.selector.SingleSelectedThing: VehiclePawn })
    {
      __result = true;
      return false;
    }
    return true;
  }

  private static bool DontRenderRimHUDForVehicles_InspectPaneFiller(ISelectable sel,
    ref bool __result)
  {
    if (sel is VehiclePawn)
    {
      __result = true;
      return false;
    }
    return true;
  }
}