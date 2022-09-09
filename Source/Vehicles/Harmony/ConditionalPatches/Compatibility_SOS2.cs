using System;
using HarmonyLib;
using Verse;
using SmashTools;

namespace Vehicles
{
	internal class Compatibility_SOS2 : IConditionalPatch
	{
		public void PatchAll(ModMetaData mod, Harmony harmony)
		{
		}

		public static bool Active { get; set; }

		public string PackageId => ConditionalPatchApplier.SOS2;
	}
}
