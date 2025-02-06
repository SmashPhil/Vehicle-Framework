using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UpdateLogTool;
using System.Diagnostics;
using SmashTools.Debugging;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	internal static class VehicleHarmony
	{
		// Project Start Date: DEC 7 2019

		public const string VehiclesUniqueId = "SmashPhil.VehicleFramework";
		public const string VehiclesLabel = "Vehicle Framework";
		internal const string LogLabel = "[VehicleFramework]";

		internal static ModMetaData VehicleMMD;
		internal static ModContentPack VehicleMCP;

		private static string methodPatching = string.Empty;
		
		public static readonly GridOwners gridOwners = new GridOwners();

		internal static List<UpdateLog> updates = new List<UpdateLog>();
		
		internal static Harmony Harmony { get; private set; } = new Harmony(VehiclesUniqueId);

		internal static string VersionPath => Path.Combine(VehicleMMD.RootDir.FullName, "Version.txt");

		internal static string BuildDatePath => Path.Combine(VehicleMMD.RootDir.FullName, "BuildDate.txt");

		public static List<VehicleDef> AllMoveableVehicleDefs { get; internal set; }

		public static List<VehicleDef> AllVehicleOwners => gridOwners.Owners;

		static VehicleHarmony()
		{
			//harmony.PatchAll(Assembly.GetExecutingAssembly());
			//Harmony.DEBUG = true;

			VehicleMCP = VehicleMod.mod.Content;
			VehicleMMD = ModLister.GetActiveModWithIdentifier(VehiclesUniqueId, ignorePostfix: true);

			Log.Message($"<color=orange>{LogLabel}</color> version {VehicleMMD.ModVersion}");

			Harmony.PatchAll();

			RunAllPatches();

			Utilities.InvokeWithLogging(ResolveAllReferences);
			Utilities.InvokeWithLogging(PostDefDatabaseCalls);
			Utilities.InvokeWithLogging(RegisterDisplayStats);

			Utilities.InvokeWithLogging(RegisterKeyBindingDefs);

			//Will want to be added via xml
			Utilities.InvokeWithLogging(FillVehicleLordJobTypes);

			Utilities.InvokeWithLogging(ApplyAllDefModExtensions);
			Utilities.InvokeWithLogging(PathingHelper.LoadTerrainTagCosts);
			Utilities.InvokeWithLogging(PathingHelper.LoadTerrainDefaults);
			Utilities.InvokeWithLogging(RecacheMoveableVehicleDefs);
			Utilities.InvokeWithLogging(PathingHelper.CacheVehicleRegionEffecters);

			Utilities.InvokeWithLogging(LoadedModManager.GetMod<VehicleMod>().InitializeTabs);
			Utilities.InvokeWithLogging(VehicleMod.settings.Write);

			Utilities.InvokeWithLogging(RegisterTweakFieldsInEditor);
			Utilities.InvokeWithLogging(PatternDef.GenerateMaterials);

			Utilities.InvokeWithLogging(RegisterVehicleAreas);

#if DEBUG
			//UnitTestManager.onUnitTestStateChange += ForceSynchronousMaps;
			UnitTestManager.onUnitTestStateChange += SuppressDebugLogging;
#endif

			if (DebugProperties.debug)
			{
				//DebugHelper.Local.VehicleDef = DefDatabase<VehicleDef>.GetNamedSilentFail("VF_TestMarshal");
				if (DebugHelper.Local.VehicleDef != null)
				{
					//DebugHelper.Local.DebugType = DebugRegionType.Regions;
				}
			}
		}

		private static void RunAllPatches()
		{
			List<IPatchCategory> patchCategories = new List<IPatchCategory>();
			foreach (Assembly assembly in VehicleMCP.assemblies.loadedAssemblies)
			{
				foreach (Type type in assembly.GetTypes())
				{
					if (type.HasInterface(typeof(IPatchCategory)))
					{
						IPatchCategory patch = (IPatchCategory)Activator.CreateInstance(type, null);
						patchCategories.Add(patch);
					}
				}
			}
			foreach (IPatchCategory patch in patchCategories)
			{
				try
				{
					patch.PatchMethods();
				}
				catch
				{
					SmashLog.Error($"Failed to Patch <type>{patch.GetType().FullName}</type>. Method=\"{methodPatching}\"");
					throw;
				}
			}
			if (Prefs.DevMode) SmashLog.Message($"<color=orange>{LogLabel}</color> <success>{Harmony.GetPatchedMethods().Count()} patches successfully applied.</success>");
		}
		
		public static void Patch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null, HarmonyMethod finalizer = null)
		{
			methodPatching = original?.Name ?? $"Null\", Previous = \"{methodPatching}";
			Harmony.Patch(original, prefix, postfix, transpiler, finalizer);
		}

		public static void ResolveAllReferences()
		{
			foreach (var defFields in VehicleMod.settings.upgrades.upgradeSettings.Values)
			{
				foreach (SaveableField field in defFields.Keys)
				{
					field.ResolveReferences();
				}
			}
		}

		public static void PostDefDatabaseCalls()
		{
			VehicleMod.settings.main.PostDefDatabase();
			VehicleMod.settings.vehicles.PostDefDatabase();
			VehicleMod.settings.upgrades.PostDefDatabase();
			VehicleMod.settings.debug.PostDefDatabase();
			foreach (VehicleDef def in DefDatabase<VehicleDef>.AllDefsListForReading)
			{
				def.PostDefDatabase();
				foreach (CompProperties compProperties in def.comps)
				{
					if (compProperties is VehicleCompProperties vehicleCompProperties)
					{
						vehicleCompProperties.PostDefDatabase();
					}
				}
			}
			foreach (VehicleTurretDef turretDef in DefDatabase<VehicleTurretDef>.AllDefsListForReading)
			{
				turretDef.PostDefDatabase();
			}
		}

		private static void RegisterDisplayStats()
		{
			VehicleInfoCard.RegisterStatDef(StatDefOf.Flammability);

			//VehicleInfoCard.RegisterStatDef(StatDefOf.RestRateMultiplier);
			//VehicleInfoCard.RegisterStatDef(StatDefOf.Comfort);
			//VehicleInfoCard.RegisterStatDef(StatDefOf.Insulation_Cold);
			//VehicleInfoCard.RegisterStatDef(StatDefOf.Insulation_Heat);
			//VehicleInfoCard.RegisterStatDef(StatDefOf.SellPriceFactor);
		}

		private static void RegisterKeyBindingDefs()
		{
			MainMenuKeyBindHandler.RegisterKeyBind(KeyBindingDefOf_Vehicles.VF_RestartGame, GenCommandLine.Restart);
			MainMenuKeyBindHandler.RegisterKeyBind(KeyBindingDefOf_Vehicles.VF_QuickStartMenu, () => StartupTest.OpenMenu());
			MainMenuKeyBindHandler.RegisterKeyBind(KeyBindingDefOf_Vehicles.VF_DebugSettings, () => VehiclesModSettings.OpenWithContext());
		}

		private static void FillVehicleLordJobTypes()
		{
			VehicleIncidentSwapper.RegisterLordType(typeof(LordJob_ArmoredAssault));
		}

		public static void ClearModConfig()
		{
			Utilities.DeleteConfig(VehicleMod.mod);
		}

		internal static void RecacheMoveableVehicleDefs()
		{
			AllMoveableVehicleDefs = DefDatabase<VehicleDef>.AllDefsListForReading.Where(PathingHelper.ShouldCreateRegions).ToList();
			gridOwners.Init();
			if (!Find.Maps.NullOrEmpty())
			{
				foreach (Map map in Find.Maps)
				{
					map.GetCachedMapComponent<VehicleMapping>().ConstructComponents();
				}
			}
		}

		private static void ApplyAllDefModExtensions()
		{
			PathingHelper.LoadDefModExtensionCosts(vehicleDef => vehicleDef.properties.customThingCosts);
			PathingHelper.LoadDefModExtensionCosts(vehicleDef => vehicleDef.properties.customTerrainCosts);
			PathingHelper.LoadDefModExtensionCosts(vehicleDef => vehicleDef.properties.customBiomeCosts);
			PathingHelper.LoadDefModExtensionCosts(vehicleDef => vehicleDef.properties.customRoadCosts);
			PathingHelper.LoadDefModExtensionCosts(vehicleDef => vehicleDef.properties.customRiverCosts);
		}
		
		private static void RegisterTweakFieldsInEditor()
		{
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicData), nameof(GraphicData.drawOffset)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicData), nameof(GraphicData.drawOffsetNorth)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicData), nameof(GraphicData.drawOffsetEast)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicData), nameof(GraphicData.drawOffsetSouth)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicData), nameof(GraphicData.drawOffsetWest)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicDataLayered), nameof(GraphicData.drawOffset)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicDataLayered), nameof(GraphicData.drawOffsetNorth)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicDataLayered), nameof(GraphicData.drawOffsetEast)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicDataLayered), nameof(GraphicData.drawOffsetSouth)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicDataLayered), nameof(GraphicData.drawOffsetWest)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicDataRGB), nameof(GraphicData.drawOffset)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicDataRGB), nameof(GraphicData.drawOffsetNorth)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicDataRGB), nameof(GraphicData.drawOffsetEast)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicDataRGB), nameof(GraphicData.drawOffsetSouth)), string.Empty, string.Empty, UISettingsType.FloatBox);
			EditWindow_TweakFields.RegisterField(AccessTools.Field(typeof(GraphicDataRGB), nameof(GraphicData.drawOffsetWest)), string.Empty, string.Empty, UISettingsType.FloatBox);
		}

		private static void RegisterVehicleAreas()
		{
			Ext_Map.RegisterArea<Area_Road>();
			Ext_Map.RegisterArea<Area_RoadAvoidal>();
		}

		/// <summary>
		/// Force maps to operate synchronously for the duration of unit tests
		/// </summary>
		private static void ForceSynchronousMaps(bool value)
		{
			if (Current.ProgramState == ProgramState.Entry) return;

			foreach (Map map in Find.Maps)
			{
				VehicleMapping mapping = map.GetCachedMapComponent<VehicleMapping>();
				if (value)
				{
					mapping.ReleaseThread();
				}
				else
				{
					mapping.InitThread(map);
				}
			}
		}

		private static void SuppressDebugLogging(bool value)
		{
			VehicleMod.settings.debug.debugLogging = false;
		}
	}
}