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
		static ConditionalPatchApplier()
		{
			var harmony = new Harmony("conditional_patches.rimworld.smashphil");

			ConditionalPatches.PatchAllActiveMods(harmony, VehicleHarmony.VehiclesUniqueId);
		}
	}
}
