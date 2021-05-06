using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class Laser : Bullet
	{
		private SpawnerPropertiesDefModExtension modExtension;

		public SpawnerPropertiesDefModExtension SpawnProps
		{
			get
			{
				if (modExtension is null)
				{
					modExtension = def.GetModExtension<SpawnerPropertiesDefModExtension>();
					if (modExtension is null)
					{
						Log.Error($"Must include <type>SpawnerPropertiesDefModExtension</type> for projectile of type <type>Laser</type>");
					}
				}
				return modExtension;
			}
		}

		protected override void Impact(Thing hitThing)
		{
			Map map = Map;
			IntVec3 position = Position;
			base.Impact(hitThing);

			if (Rand.Range(0, 1) <= SpawnProps.chanceToSpawnThing)
			{
				if (SpawnProps.thingToSpawn.defName == "Fire")
				{
					float fireSize = Rand.Range(0.25f, 0.925f);
					if (!FireUtility.TryStartFireIn(position, map, fireSize))
					{
						foreach (Thing thing in map.thingGrid.ThingsAt(position))
						{
							thing.TryAttachFire(fireSize);
						}
					}
				}
				else
				{
					GenSpawn.Spawn(SpawnProps.thingToSpawn, position, map);
				}
			}
		}
	}
}
