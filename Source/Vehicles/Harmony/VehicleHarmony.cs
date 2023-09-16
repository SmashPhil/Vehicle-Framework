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

namespace Vehicles
{
	[StaticConstructorOnStartup]
	internal static class VehicleHarmony
	{
		private const int BuildMajor = 1;
		private const int BuildMinor = 5;

		private static readonly DateTime ProjectStartDate = new DateTime(2019, 12, 7);

		public const string VehiclesUniqueId = "SmashPhil.VehicleFramework";
		public const string VehiclesLabel = "Vehicle Framework";
		internal const string LogLabel = "[VehicleFramework]";

		internal static ModMetaData VehicleMMD;
		internal static ModContentPack VehicleMCP;

		/// <summary>
		/// Debugging
		/// </summary>
		internal static List<WorldPath> debugLines = new List<WorldPath>();
		internal static List<Pair<int, int>> tiles = new List<Pair<int, int>>(); // Pair -> TileID : Cycle
		internal static readonly bool debug = false;
		internal static readonly bool drawPaths = false;

		private static string methodPatching = string.Empty;

		internal static List<UpdateLog> updates = new List<UpdateLog>();

		internal static Harmony Harmony { get; private set; } = new Harmony(VehiclesUniqueId);

		internal static ModVersion Version { get; private set; }

		internal static string VersionPath => Path.Combine(VehicleMMD.RootDir.FullName, "Version.txt");

		internal static string BuildDatePath => Path.Combine(VehicleMMD.RootDir.FullName, "BuildDate.txt");

		public static List<VehicleDef> AllMoveableVehicleDefs { get; internal set; }

		static VehicleHarmony()
		{
			//harmony.PatchAll(Assembly.GetExecutingAssembly());
			//Harmony.DEBUG = true;

			VehicleMCP = VehicleMod.settings.Mod.Content;
			VehicleMMD = ModLister.GetActiveModWithIdentifier(VehiclesUniqueId, ignorePostfix: true);

			try
			{
				string dateText = File.ReadAllText(BuildDatePath).Trim(Environment.NewLine.ToCharArray());
				//DateTime buildDate = DateTime.ParseExact(dateText, "ddd MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);

				///Manually parsed because Linux and Mac be weird even with invariant culture, and it's just easier to do it this way
				string month = dateText.Substring(4, 2);
				string day = dateText.Substring(7, 2);
				string year = dateText.Substring(10, 4);
				string hour = dateText.Substring(15, 2);
				string minute = dateText.Substring(18, 2);
				string second = dateText.Substring(21, 2);
				DateTime buildDate = new DateTime(int.Parse(year), int.Parse(month), int.Parse(day), int.Parse(hour), int.Parse(minute), int.Parse(second));
				Version = new ModVersion(BuildMajor, BuildMinor, buildDate, ProjectStartDate);

				string readout = Prefs.DevMode ? Version.VersionStringWithRevision : Version.VersionString;
				Log.Message($"<color=orange>{LogLabel}</color> version {readout}");

				File.WriteAllText(VersionPath, Version.VersionString);
			}
			catch(Exception ex)
			{
				Log.Error($"Exception thrown while attempting to parse VehicleFramework version number. \nException={ex}");
			}

			Harmony.PatchAll();

			IEnumerable <Type> patchCategories = GenTypes.AllTypes.Where(t => t.GetInterfaces().Contains(typeof(IPatchCategory)));
			foreach (Type patchCategory in patchCategories)
			{
				IPatchCategory patch = (IPatchCategory)Activator.CreateInstance(patchCategory, null);
				try
				{
					patch.PatchMethods();
				}
				catch (Exception ex)
				{
					SmashLog.Error($"Failed to Patch <type>{patch.GetType().FullName}</type>. Method=\"{methodPatching}\"");
					throw ex;
				}
			}
			if (Prefs.DevMode) SmashLog.Message($"<color=orange>{LogLabel}</color> <success>{Harmony.GetPatchedMethods().Count()} patches successfully applied.</success>");

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

		public static void RegisterDisplayStats()
		{
			VehicleInfoCard.RegisterStatDef(StatDefOf.Flammability);

			//VehicleInfoCard.RegisterStatDef(StatDefOf.RestRateMultiplier);
			//VehicleInfoCard.RegisterStatDef(StatDefOf.Comfort);
			//VehicleInfoCard.RegisterStatDef(StatDefOf.Insulation_Cold);
			//VehicleInfoCard.RegisterStatDef(StatDefOf.Insulation_Heat);
			//VehicleInfoCard.RegisterStatDef(StatDefOf.SellPriceFactor);
		}

		public static void RegisterKeyBindingDefs()
		{
			MainMenuKeyBindHandler.RegisterKeyBind(KeyBindingDefOf_Vehicles.VF_RestartGame, GenCommandLine.Restart);
			MainMenuKeyBindHandler.RegisterKeyBind(KeyBindingDefOf_Vehicles.VF_QuickStartMenu, () => Find.WindowStack.Add(new QuickStartMenu()));
			MainMenuKeyBindHandler.RegisterKeyBind(KeyBindingDefOf_Vehicles.VF_DebugSettings, () => VehiclesModSettings.OpenWithContext());
		}

		public static void FillVehicleLordJobTypes()
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
	}
}