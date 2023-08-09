using System;
using HarmonyLib;
using Verse;
using SmashTools;
using UnityEngine;
using RimWorld;

namespace Vehicles
{
	internal class Compatibility_GeologicalLandforms : ConditionalVehiclePatch
	{
		public override string PackageId => CompatibilityPackageIds.GeologicalLandforms;

		public override void PatchAll(ModMetaData mod, Harmony harmony)
		{
		}
	}
}
