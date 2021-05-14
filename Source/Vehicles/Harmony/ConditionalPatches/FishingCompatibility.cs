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
	[StaticConstructorOnStartup]
	internal static class FishingCompatibility
	{
		public static Dictionary<ThingDef, int> fishDictionaryTemperateBiomeFreshWater = new Dictionary<ThingDef, int>();
		public static Dictionary<ThingDef, int> fishDictionaryTropicalBiomeFreshWater = new Dictionary<ThingDef, int>();
		public static Dictionary<ThingDef, int> fishDictionaryColdBiomeFreshWater = new Dictionary<ThingDef, int>();
		public static Dictionary<ThingDef, int> fishDictionarySaltWater = new Dictionary<ThingDef, int>();
		public static bool fishingActivated;

		static FishingCompatibility()
		{

		}
	}
}
