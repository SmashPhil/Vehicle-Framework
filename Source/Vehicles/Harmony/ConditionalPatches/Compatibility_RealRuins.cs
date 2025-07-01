using System;
using HarmonyLib;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_RealRuins : ConditionalVehiclePatch
{
  public override string PackageId => ModPackageIds.RealRuins;

  public override void PatchAll(ModMetaData mod, Harmony harmony)
  {
    Type ruinsObject_AbandonedBase = AccessTools.TypeByName("RealRuins.AbandonedBaseWorldObject");
    AerialVehicleCompatibility.AddObject(ruinsObject_AbandonedBase);
    Type ruinsObject_SmallRuins = AccessTools.TypeByName("RealRuins.SmallRuinsWorldObject");
    AerialVehicleCompatibility.AddObject(ruinsObject_SmallRuins);
    Type ruinsObject_POI = AccessTools.TypeByName("RealRuins.RealRuinsPOIWorldObject");
    AerialVehicleCompatibility.AddObject(ruinsObject_POI);
  }
}