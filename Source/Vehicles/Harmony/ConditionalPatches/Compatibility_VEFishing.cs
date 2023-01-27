using System;
using HarmonyLib;
using Verse;
using SmashTools;

namespace Vehicles
{
	internal class Compatibility_VEFishing : ConditionalVehiclePatch
	{
		public override void PatchAll(ModMetaData mod, Harmony harmony)
		{
		}

		public override string PackageId => CompatibilityPackageIds.VE_Fishing;
	}
}
