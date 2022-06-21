using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	[Obsolete]
	public class SpawnerPropertiesDefModExtension : DefModExtension
	{
		public float chanceToSpawnThing = 1;
		public ThingDef thingToSpawn;
	}
}
