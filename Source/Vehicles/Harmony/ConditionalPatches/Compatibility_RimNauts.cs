using System;
using HarmonyLib;
using Verse;
using SmashTools;

namespace Vehicles
{
	internal class Compatibility_RimNauts : ConditionalVehiclePatch
	{
		public override string PackageId => CompatibilityPackageIds.RimNauts;

		public override void PatchAll(ModMetaData mod, Harmony harmony)
		{
		}
	}
}
