using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Steam;
using RimWorld;

namespace Vehicles
{
	public class SavedSettings : WorkshopItem
	{
		public string name;

		public void ExposeData()
		{
			Scribe_Values.Look(ref name, nameof(name));
		}
	}
}
