using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class VehiclesModSettings : ModSettings
	{
		// Not displayed in ModSettings
		public bool showAllCargoItems;

		public Section_Main main = new Section_Main();
		public Section_Vehicles vehicles = new Section_Vehicles();
		public Section_Upgrade upgrades = new Section_Upgrade();
		public Section_Debug debug = new Section_Debug();

		//Color Palettes
		public ColorStorage colorStorage = new ColorStorage();

		public override void ExposeData()
		{
			try
			{
				Scribe_Deep.Look(ref main, "main");
				Scribe_Deep.Look(ref vehicles, "vehicles");
				//Scribe_Deep.Look(ref upgrades, "upgrades");
				Scribe_Deep.Look(ref debug, "debug");

				Scribe_Values.Look(ref showAllCargoItems, "showAllCargoItems");
				Scribe_Deep.Look(ref colorStorage, "colorStorage");
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown while trying to load mod settings. Deleting the Vehicles config file might fix this.\nException={ex.Message}\nInnerException={ex.InnerException}");
			}
		}

		public static void Open()
		{
			Dialog_ModSettings settings = new Dialog_ModSettings(VehicleMod.mod);
			Find.WindowStack.Add(settings);
		}

		public static void OpenWithContext(SettingsSection section = null)
		{
			Open();
			if (section != null)
			{
				VehicleMod.CurrentSection = section;
			}
			else if (!WorldRendererUtility.WorldRenderedNow && Find.CurrentMap != null && Find.Selector.SelectedObjects.FirstOrDefault() is VehiclePawn vehicle)
			{
				VehicleMod.CurrentSection = VehicleMod.settings.vehicles;
				VehicleMod.SelectVehicle(vehicle.VehicleDef);
			}
		}
	}
}
