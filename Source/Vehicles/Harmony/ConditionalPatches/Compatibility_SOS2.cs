using HarmonyLib;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_SoS2 : ConditionalVehiclePatch
{
  public override string PackageId => ModPackageIds.SoS2;

  public override void PatchAll(ModMetaData mod, Harmony harmony)
  {
  }
}