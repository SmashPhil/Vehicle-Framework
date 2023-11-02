using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Globalization;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	internal static class FishingCompatibility
	{
		private static List<ThingDef> tmpFishes = new List<ThingDef>();

		public static bool Active { get; private set; }

		internal static void EnableFishing()
		{
			Active = true;

			foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefsListForReading.Where(thingDef => thingDef.ingestible != null))
			{
				tmpFishes.Add(thingDef);
			}
		}

		internal static ThingDef FetchViableFish(BiomeDef biomeDef, TerrainDef terrainDef)
		{
			return tmpFishes.RandomElement();
		}
	}
}
