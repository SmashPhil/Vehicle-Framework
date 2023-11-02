using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public static class ProjectileSkyfallerMaker
	{
		/// <summary>
		/// Generate projectile launched from <paramref name="origin"/> to <paramref name="destination"/>
		/// </summary>
		/// <param name="def"></param>
		/// <param name="vehicle"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		public static ProjectileSkyfaller WrapProjectile(ThingDef skyfallerDef, ThingDef projectileDef, Thing caster, Vector3 origin, Vector3 destination, float speedTilesPerTick, bool reverseDraw = false)
		{
			try
			{
				ProjectileSkyfaller skyfaller = (ProjectileSkyfaller)ThingMaker.MakeThing(skyfallerDef);
				skyfaller.caster = caster;
				skyfaller.origin = origin;
				skyfaller.destination = destination;
				skyfaller.projectileDef = projectileDef;
				skyfaller.speedTilesPerTick = speedTilesPerTick;
				skyfaller.reverseDraw = reverseDraw;
				return skyfaller;
			}
			catch (Exception ex)
			{
				Log.Error($"Unable to generate StrafeProjectile with projectile <type>{projectileDef.thingClass}</type>. Exception=\"{ex}\"");
			}
			return null;
		}
	}
}
