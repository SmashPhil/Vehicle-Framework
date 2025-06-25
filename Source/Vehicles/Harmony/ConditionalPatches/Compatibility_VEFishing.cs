using HarmonyLib;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_VEFishing : ConditionalVehiclePatch
{
  public override void PatchAll(ModMetaData mod, Harmony harmony)
  {
    //FishingCompatibility.EnableFishing();
  }

  public override string PackageId => CompatibilityPackageIds.VE_Fishing;
}