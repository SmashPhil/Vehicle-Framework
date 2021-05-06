using System;
using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class VehiclesModSettings : ModSettings
	{
		/* Not displayed in ModSettings */
		public bool showAllCargoItems;

		public Section_Main main = new Section_Main();
		public Section_Vehicles vehicles = new Section_Vehicles();
		public Section_Upgrade upgrades = new Section_Upgrade();
		public Section_Debug debug = new Section_Debug();

		public ColorStorage colorStorage = new ColorStorage();

		/* ---------------------------------- */

		public override void ExposeData()
		{
			try
			{
				Scribe_Deep.Look(ref main, "main");
				Scribe_Deep.Look(ref vehicles, "vehicles");
				Scribe_Deep.Look(ref upgrades, "upgrades");
				Scribe_Deep.Look(ref debug, "debug");

				Scribe_Values.Look(ref showAllCargoItems, "showAllCargoItems");
				Scribe_Deep.Look(ref colorStorage, "colorStorage");
			}
			catch (Exception ex)
			{
				Log.Error($"Exception thrown while trying to load mod settings. Deleting the Vehicles config file might fix this.\nException={ex.Message}\nInnerException={ex.InnerException}");
			}
		}
	}
}
