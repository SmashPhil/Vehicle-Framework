using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using UpdateLog;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	internal static class VehicleHarmony
	{
		public const string VehiclesUniqueId = "smashphil.vehicles";
		internal const string LogLabel = "[Vehicles]";

		/// <summary>
		/// Debugging
		/// </summary>
		internal static List<WorldPath> debugLines = new List<WorldPath>();
		internal static List<Pair<int, int>> tiles = new List<Pair<int, int>>(); // Pair -> TileID : Cycle
		internal const bool debug = false;
		internal const bool drawPaths = false;

		private static string methodPatching = string.Empty;

		internal static Harmony Harmony { get; private set; } = new Harmony(VehiclesUniqueId);

		static VehicleHarmony()
		{
			//harmony.PatchAll(Assembly.GetExecutingAssembly());
			//Harmony.DEBUG = true;
			Log.Message($"{LogLabel} version {Assembly.GetExecutingAssembly().GetName().Version}");

			IEnumerable<Type> patchCategories = GenTypes.AllTypes.Where(t => t.GetInterfaces().Contains(typeof(IPatchCategory)));
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

		public static void RegisterUpdateVersion()
		{
			try
			{
				UpdateLog.UpdateLog log = UpdateHandler.modUpdates.FirstOrDefault(u => u.Mod == ConditionalPatchApplier.VehicleMCP);
				VehicleMod.settings.debug.updateLogs ??= new Dictionary<string, string>();
				if (!VehicleMod.settings.debug.updateLogs.ContainsKey(log.UpdateData.currentVersion))
				{
					VehicleMod.settings.debug.updateLogs.Add(log.UpdateData.currentVersion, log.UpdateData.description);
				}
				else
				{
					VehicleMod.settings.debug.updateLogs[log.UpdateData.currentVersion] = log.UpdateData.description;
				}
			}
			catch (Exception ex)
			{
				Log.Warning($"Unable to register update for backtracking. Exception = {ex.Message}");
			}
		}
		
		public static void OpenBetaDialog()
		{
#if BETA
			Find.WindowStack.Add(new Dialog_BetaWindow());
#endif
		}
	}
}