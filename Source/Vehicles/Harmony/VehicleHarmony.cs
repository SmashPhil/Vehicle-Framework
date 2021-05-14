using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

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
		private static readonly Harmony harmony;

		static VehicleHarmony()
		{
			harmony = new Harmony(VehiclesUniqueId);
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
			SmashLog.Message($"{LogLabel} <success>{harmony.GetPatchedMethods().Count()} patches successfully applied.</success>");

			ResolveAllReferences();
			//Will want to be added via xml
			FillVehicleLordJobTypes();

			LoadedModManager.GetMod<VehicleMod>().InitializeTabs();
			VehicleMod.settings.Write();
		}
		
		public static void Patch(MethodBase original, HarmonyMethod prefix = null, HarmonyMethod postfix = null, HarmonyMethod transpiler = null, HarmonyMethod finalizer = null)
		{
			methodPatching = original.Name;
			harmony.Patch(original, prefix, postfix, transpiler, finalizer);
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

		#if BETA
		public static void OpenBetaDialog()
		{
			Find.WindowStack.Add(new Dialog_BetaWindow());
		}
		#endif
	}
}