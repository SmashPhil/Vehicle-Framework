using HarmonyLib;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_GeologicalLandforms : ConditionalVehiclePatch
{
  public override string PackageId => CompatibilityPackageIds.GeologicalLandforms;

  public override void PatchAll(ModMetaData mod, Harmony harmony)
  {
  }
}