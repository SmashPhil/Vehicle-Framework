using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UpdateLogTool;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	internal static class VehicleHarmony
	{
		public const string VehiclesUniqueId = "smashphil.vehicles";
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

		public static string CurrentVersion { get; private set; }

		internal static Harmony Harmony { get; private set; } = new Harmony(VehiclesUniqueId);

		internal static string VersionDir => Path.Combine(VehicleMMD.RootDir.FullName, "Version.txt");

		static VehicleHarmony()
		{
			//harmony.PatchAll(Assembly.GetExecutingAssembly());
			//Harmony.DEBUG = true;

			VehicleMCP = VehicleMod.settings.Mod.Content;
			VehicleMMD = ModLister.GetActiveModWithIdentifier(VehiclesUniqueId);

			Version version = Assembly.GetExecutingAssembly().GetName().Version;
			CurrentVersion = $"{version.Major}.{version.Minor}.{version.Build}";
			Log.Message($"{LogLabel} version {CurrentVersion}");

			File.WriteAllText(VersionDir, CurrentVersion);

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
			SmashLog.Message($"{LogLabel} <success>{Harmony.GetPatchedMethods().Count()} patches successfully applied.</success>");

			ResolveAllReferences();
			//Will want to be added via xml
			FillVehicleLordJobTypes();

			LoadedModManager.GetMod<VehicleMod>().InitializeTabs();
			VehicleMod.settings.Write();
		}
		
		public static void Patch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null, HarmonyMethod finalizer = null)
		{
			methodPatching = original.Name;
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

		//REDO
		public static void FillVehicleLordJobTypes()
		{
			VehicleIncidentSwapper.RegisterLordType(typeof(LordJob_ArmoredAssault));
		}
		
		public static void OpenBetaDialog()
		{
#if BETA
			Find.WindowStack.Add(new Dialog_BetaWindow());
#endif
		}
	}
}