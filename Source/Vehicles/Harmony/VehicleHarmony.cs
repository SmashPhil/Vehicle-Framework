using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using RimWorld;
using SmashTools;
using UnityEngine.Assertions;
using UpdateLogTool;
using Vehicles.Config;
using Verse;
using static Vehicles.Config.FeatureFlags;

namespace Vehicles;

[StaticConstructorOnStartup]
public static class VehicleHarmony
{
	// Project Start Date: 7 DEC 2019

	public const string VehiclesUniqueId = "SmashPhil.VehicleFramework";
	public const string VehiclesLabel = "Vehicle Framework";
	internal const string LogLabel = "[VehicleFramework]";

	internal static List<UpdateLog> updates = [];

	internal static string VersionPath =>
		Path.Combine(VehicleMod.metaData.RootDir.FullName, "Version.txt");

	internal static string BuildDatePath =>
		Path.Combine(VehicleMod.metaData.RootDir.FullName, "BuildDate.txt");

	public static List<VehicleDef> AllMoveableVehicleDefs { get; internal set; }

	public static bool BurstEnabled { get; }

	static VehicleHarmony()
	{
		Assert.IsTrue(UnityData.IsInMainThread);

		UpdateHandler.LoadUpdateLog(VehicleMod.content);

		using DeepProfilerScope dps = new(nameof(VehicleHarmony));
#if !RELEASE
		Log.Message($"{LogLabel} version {VehicleMod.metaData.ModVersion} -DEV");
#else
    Log.Message($"{LogLabel} version {VehicleMod.metaData.ModVersion}");
#endif

#if DEBUG
		if (IsFeatureEnabled(BurstLib))
		{
			BurstEnabled = SmashTools.Burst.BurstAssemblyLoader.LoadAllFor(VehicleMod.content);
		}
#endif

		Utilities.InvokeWithLogging(ResolveAllReferences);
		Utilities.InvokeWithLogging(PostDefDatabaseCalls);
		Utilities.InvokeWithLogging(RegisterDisplayStats);

		Utilities.InvokeWithLogging(RegisterKeyBindingDefs);

		// TODO - Will want to be added via xml
		Utilities.InvokeWithLogging(FillVehicleLordJobTypes);

		Utilities.InvokeWithLogging(ApplyAllDefModExtensions);
		Utilities.InvokeWithLogging(PathingHelper.LoadTerrainTagCosts);
		Utilities.InvokeWithLogging(PathingHelper.LoadTerrainDefaults);
		Utilities.InvokeWithLogging(GridOwners.RecacheMoveableVehicleDefs);
		Utilities.InvokeWithLogging(PathingHelper.CacheVehicleRegionEffecters);

		Utilities.InvokeWithLogging(LoadedModManager.GetMod<VehicleMod>().InitializeTabs);
		Utilities.InvokeWithLogging(VehicleMod.settings.Write);

		Utilities.InvokeWithLogging(RegisterTweakFieldsInEditor);
		Utilities.InvokeWithLogging(PatternDef.GenerateMaterials);

		DebugProperties.Init();
	}

	private static void ResolveAllReferences()
	{
		foreach (Dictionary<SaveableField, SavedField<object>> defFields in VehicleMod.settings.upgrades
		 .upgradeSettings.Values)
		{
			foreach (SaveableField field in defFields.Keys)
			{
				field.ResolveReferences();
			}
		}
	}

	private static void PostDefDatabaseCalls()
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

		foreach (VehicleTurretDef def in DefDatabase<VehicleTurretDef>.AllDefsListForReading)
		{
			def.PostDefDatabase();
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
		MainMenuKeyBindHandler.RegisterKeyBind(KeyBindingDefOf_Vehicles.VF_RestartGame,
			GenCommandLine.Restart);
		MainMenuKeyBindHandler.RegisterKeyBind(KeyBindingDefOf_Vehicles.VF_QuickStartMenu,
			() => StartupTest.OpenMenu());
		MainMenuKeyBindHandler.RegisterKeyBind(KeyBindingDefOf_Vehicles.VF_DebugSettings,
			() => VehiclesModSettings.OpenWithContext());
	}

	private static void FillVehicleLordJobTypes()
	{
		VehicleIncidentSwapper.RegisterLordType(typeof(LordJob_ArmoredAssault));
	}

	public static void ClearModConfig()
	{
		Utilities.DeleteConfig(VehicleMod.mod);
	}

	private static void ApplyAllDefModExtensions()
	{
		PathingHelper.LoadDefModExtensionCosts(vehicleDef => vehicleDef.properties.customThingCosts);
		PathingHelper.LoadDefModExtensionCosts(vehicleDef =>
			vehicleDef.properties.customTerrainCosts);
		PathingHelper.LoadDefModExtensionCosts(vehicleDef => vehicleDef.properties.customBiomeCosts);
		PathingHelper.LoadDefModExtensionCosts(vehicleDef => vehicleDef.properties.customRoadCosts);
		PathingHelper.LoadDefModExtensionCosts(vehicleDef => vehicleDef.properties.customRiverCosts);
	}

	private static void RegisterTweakFieldsInEditor()
	{
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(Graphic), nameof(Graphic.data)), string.Empty, string.Empty, UISettingsType.None);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicData), nameof(GraphicData.drawOffset)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicData), nameof(GraphicData.drawOffsetNorth)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicData), nameof(GraphicData.drawOffsetEast)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicData), nameof(GraphicData.drawOffsetSouth)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicData), nameof(GraphicData.drawOffsetWest)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicDataLayered), nameof(GraphicData.drawOffset)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicDataLayered), nameof(GraphicData.drawOffsetNorth)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicDataLayered), nameof(GraphicData.drawOffsetEast)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicDataLayered), nameof(GraphicData.drawOffsetSouth)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicDataLayered), nameof(GraphicData.drawOffsetWest)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicDataRGB), nameof(GraphicData.drawOffset)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicDataRGB), nameof(GraphicData.drawOffsetNorth)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicDataRGB), nameof(GraphicData.drawOffsetEast)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicDataRGB), nameof(GraphicData.drawOffsetSouth)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
		EditWindow_TweakFields.RegisterField(
			AccessTools.Field(typeof(GraphicDataRGB), nameof(GraphicData.drawOffsetWest)),
			string.Empty, string.Empty, UISettingsType.FloatBox);
	}
}