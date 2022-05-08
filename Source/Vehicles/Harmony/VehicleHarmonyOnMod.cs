using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	[LoadedEarly]
	[StaticConstructorOnModInit]
	public static class VehicleHarmonyOnMod
	{
		static VehicleHarmonyOnMod()
		{
			ParsingHelper.RegisterParsers();
			ParsingHelper.RegisterAttributes();

			var harmony = new Harmony($"{VehicleHarmony.VehiclesUniqueId}_preload");

			harmony.Patch(original: AccessTools.Property(type: typeof(RaceProperties), name: nameof(RaceProperties.IsFlesh)).GetGetMethod(),
				prefix: new HarmonyMethod(typeof(VehicleHarmonyOnMod),
				nameof(VehiclesDontHaveFlesh)));
			harmony.Patch(original: AccessTools.Method(typeof(ThingDef), nameof(ThingDef.ConfigErrors)), prefix: null,
				postfix: new HarmonyMethod(typeof(VehicleHarmonyOnMod),
				nameof(VehiclesAllowFullFillage)));
			harmony.Patch(original: AccessTools.PropertyGetter(typeof(ShaderTypeDef), nameof(ShaderTypeDef.Shader)),
				prefix: new HarmonyMethod(typeof(VehicleHarmonyOnMod),
				nameof(ShaderFromAssetBundle)));
			harmony.Patch(original: AccessTools.Method(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve)),
				postfix: new HarmonyMethod(typeof(VehicleHarmonyOnMod),
				nameof(ImpliedDefGeneratorVehicles)));
			harmony.Patch(original: AccessTools.Method(typeof(GraphicData), "Init"),
				prefix: new HarmonyMethod(typeof(VehicleHarmonyOnMod),
				nameof(GraphicPreInit)));
			/* Debugging Only */
			//harmony.Patch(original: AccessTools.Method(typeof(DirectXmlToObject), "GetFieldInfoForType"),
			//	prefix: new HarmonyMethod(typeof(VehicleHarmonyOnMod),
			//	nameof(TestDebug)));
		}

		/// <summary>
		/// Generic patch method for testing
		/// </summary>
		public static bool TestDebug()
		{
			return true;
		}

		/// <summary>
		/// Prevent Implied MeatDefs from being generated for flesh types of Metal, Spacer, and Wooden Vehicle
		/// </summary>
		/// <param name="__result"></param>
		/// <param name="__instance"></param>
		public static bool VehiclesDontHaveFlesh(ref bool __result, RaceProperties __instance)
		{
			if (__instance.FleshType is VehicleFleshTypeDef)
			{
				__result = false;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Remove ConfigErrors from Vehicle ThingDef with Fillage >= 1
		/// </summary>
		/// <param name="__instance"></param>
		/// <param name="__result"></param>
		public static void VehiclesAllowFullFillage(ThingDef __instance, ref IEnumerable<string> __result)
		{
			if (__instance is VehicleDef def && __result.NotNullAndAny() && def.Fillage == FillCategory.Full)
			{
				var newList = __result.ToList();
				newList.Remove("fillPercent is 1.00 but is not edifice");
				newList.Remove("gives full cover but is not a building.");
				__result = newList;
			}
		}

		/// <summary>
		/// Load shader asset for RGB shader types
		/// </summary>
		/// <param name="__instance"></param>
		/// <param name="___shaderInt"></param>
		public static void ShaderFromAssetBundle(ShaderTypeDef __instance, ref Shader ___shaderInt)
		{
			if (__instance is RGBShaderTypeDef)
			{
				___shaderInt = AssetBundleDatabase.LoadAsset<Shader>(__instance.shaderPath);
				if (___shaderInt is null)
				{
					SmashLog.Error($"Failed to load Shader from path <text>\"{__instance.shaderPath}\"</text>");
				}
			}
		}

		/// <summary>
		/// Autogenerate implied PawnKindDefs for VehicleDefs
		/// </summary>
		public static void ImpliedDefGeneratorVehicles()
		{
			foreach (VehicleDef vehicleDef in DefDatabase<VehicleDef>.AllDefs)
			{
				DefGenerator.AddImpliedDef(PawnKindDefGenerator_Vehicles.GenerateImpliedPawnKindDef(vehicleDef));
			}
		}

		/// <summary>
		/// Redirect Init calls from GraphicData to GraphicDataRGB
		/// </summary>
		/// <param name="__instance"></param>
		public static bool GraphicPreInit(GraphicData __instance)
		{
			if (__instance is GraphicDataLayered graphicDataLayered)
			{
				graphicDataLayered.PreInit();
			}
			if (__instance is GraphicDataRGB graphicDataRGB)
			{
				graphicDataRGB.Init();
				return false;
			}
			return true;
		}
	}
}
