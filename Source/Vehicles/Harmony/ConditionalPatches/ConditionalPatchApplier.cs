using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public static class ConditionalPatchApplier
	{
		internal static ModMetaData VehicleMMD;

		internal static ModContentPack VehicleMCP;

		static ConditionalPatchApplier()
		{
			var harmony = new Harmony("conditional_patches.rimworld.smashphil");

			(VehicleMMD, VehicleMCP) = ConditionalPatches.PatchAllActiveMods(harmony, VehicleHarmony.VehiclesUniqueId);
		}
	}
}
