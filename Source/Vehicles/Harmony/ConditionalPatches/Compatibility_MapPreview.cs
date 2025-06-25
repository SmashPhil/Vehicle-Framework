using HarmonyLib;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_MapPreview : ConditionalVehiclePatch
{
  public override string PackageId => CompatibilityPackageIds.MapPreview;

  public override void PatchAll(ModMetaData mod, Harmony harmony)
  {
  }
}