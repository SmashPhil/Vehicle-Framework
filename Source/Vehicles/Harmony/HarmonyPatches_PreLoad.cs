using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;
using SmashTools;
using Vehicles.Defs;

namespace Vehicles
{
	[LoadedEarly]
	public class HarmonyPatches_PreLoad : Mod
	{
		public HarmonyPatches_PreLoad(ModContentPack content) : base(content)
		{
			ParsingHelper.RegisterParsers();
			ParsingHelper.RegisterAttributes();

			var harmony = new Harmony($"{VehicleHarmony.VehiclesUniqueId}_preload");

			harmony.Patch(original: AccessTools.Property(type: typeof(RaceProperties), name: nameof(RaceProperties.IsFlesh)).GetGetMethod(),
				prefix: new HarmonyMethod(typeof(HarmonyPatches_PreLoad),
				nameof(VehiclesDontHaveFlesh)));
			harmony.Patch(original: AccessTools.Method(typeof(ThingDef), nameof(ThingDef.ConfigErrors)), prefix: null,
				postfix: new HarmonyMethod(typeof(HarmonyPatches_PreLoad),
				nameof(VehiclesAllowFullFillage)));
			harmony.Patch(original: AccessTools.PropertyGetter(typeof(ShaderTypeDef), nameof(ShaderTypeDef.Shader)),
				prefix: new HarmonyMethod(typeof(HarmonyPatches_PreLoad),
				nameof(ShaderFromAssetBundle)));
			harmony.Patch(original: AccessTools.Method(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve)),
				postfix: new HarmonyMethod(typeof(HarmonyPatches_PreLoad),
				nameof(ImpliedDefGeneratorVehicles)));
			harmony.Patch(original: AccessTools.Method(typeof(GraphicData), "Init"),
				prefix: new HarmonyMethod(typeof(HarmonyPatches_PreLoad),
				nameof(RGBInitRedirect)));
			/* Debugging Only */
			//harmony.Patch(original: AccessTools.Method(typeof(), nameof()),
			//    prefix: new HarmonyMethod(typeof(HarmonyPatches_PreLoad),
			//    nameof(TestDebug)));
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
			if (__instance.FleshType == FleshTypeDefOf_Ships.MetalVehicle || __instance.FleshType == FleshTypeDefOf_Ships.SpacerVehicle || __instance.FleshType == FleshTypeDefOf_Ships.WoodenVehicle)
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
				___shaderInt = AssetBundleDatabase.LoadAssetBundleShader(__instance.shaderPath);
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
		public static bool RGBInitRedirect(GraphicData __instance)
		{
			if (__instance is GraphicDataRGB graphicDataRGB)
			{
				graphicDataRGB.Init();
				return false;
			}
			return true;
		}
	}
}
