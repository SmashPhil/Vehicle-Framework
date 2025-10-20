using System;
using HarmonyLib;
using SmashTools.Patching;
using Verse;

namespace Vehicles.Compatibility;

internal class Compatibility_RealRuins : ConditionalVehiclePatch
{
	public override string PackageId => ModPackageIds.RealRuins;

	public override PatchSequence PatchAt => PatchSequence.Async;

	public override void PatchAll(ModMetaData mod)
	{
		Type ruinsObjectAbandonedBase = AccessTools.TypeByName("RealRuins.AbandonedBaseWorldObject");
		AerialVehicleCompatibility.RegisterWorldObjectType(ruinsObjectAbandonedBase, 
			new AerialVehicleCompatibility.Settings(true, false));
		Type ruinsObjectSmallRuins = AccessTools.TypeByName("RealRuins.SmallRuinsWorldObject");
		AerialVehicleCompatibility.RegisterWorldObjectType(ruinsObjectSmallRuins, 
			new AerialVehicleCompatibility.Settings(true, false));
		Type ruinsObjectPoi = AccessTools.TypeByName("RealRuins.RealRuinsPOIWorldObject");
		AerialVehicleCompatibility.RegisterWorldObjectType(ruinsObjectPoi, 
			new AerialVehicleCompatibility.Settings(true, false));
	}
}