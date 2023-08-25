using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using SmashTools;
using UnityEngine;
using RimWorld;

namespace Vehicles
{
	internal class Compatibility_MapPreview : ConditionalVehiclePatch
	{
		public override string PackageId => CompatibilityPackageIds.MapPreview;

		public override void PatchAll(ModMetaData mod, Harmony harmony)
		{
		}
	}
}
