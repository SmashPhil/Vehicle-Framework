using System;
using HarmonyLib;
using Verse;
using SmashTools;

namespace Vehicles.Compatibility;

internal class Compatibility_RimNauts : ConditionalVehiclePatch
{
  public override string PackageId => ModPackageIds.RimNauts;

  public override void PatchAll(ModMetaData mod, Harmony harmony)
  {
  }
}