using System;
using HarmonyLib;
using Verse;
using SmashTools;

namespace Vehicles
{
	internal class Compatibility_VEFishing : IConditionalPatch
	{
		public void PatchAll(ModMetaData mod, Harmony harmony)
		{
		}

		public static bool Active { get; set; }

		public string PackageId => ConditionalPatchApplier.VE_Fishing;
	}
}
