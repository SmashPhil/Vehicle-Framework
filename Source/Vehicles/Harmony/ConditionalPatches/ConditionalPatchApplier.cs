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
		public const string CombatExtended = "CETeam.CombatExtended";
		
		public const string SOS2 = "kentington.saveourship2";
		public const string RimNauts = "RadZerp.neoRimNauts";

		public const string VE_Fishing = "VanillaExpanded.VCEF";
		public const string DualWield = "Roolo.DualWield";

		static ConditionalPatchApplier()
		{
			var harmony = new Harmony("conditional_patches.rimworld.smashphil");
			
			ConditionalPatches.PatchAllActiveMods(harmony, VehicleHarmony.VehiclesUniqueId);
		}
	}
}
