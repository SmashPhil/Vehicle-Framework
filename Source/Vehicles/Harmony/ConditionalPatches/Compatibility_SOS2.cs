using System;
using HarmonyLib;
using Verse;
using SmashTools;

namespace Vehicles
{
	internal class Compatibility_SOS2 : ConditionalVehiclePatch
	{
		public override string PackageId => CompatibilityPackageIds.SOS2;

		public override void PatchAll(ModMetaData mod, Harmony harmony)
		{
		}
	}
}
