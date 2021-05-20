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
		/// Generate VehicleSkyfaller_FlyOver with preassigned <paramref name="vehicle"/> from <paramref name="start"/> to <paramref name="end"/>
		/// </summary>
		/// <param name="def"></param>
		/// <param name="vehicle"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <returns></returns>
		public static StrafeProjectile WrapProjectile(ThingDef projectileDef, VehicleSkyfaller_Strafe strafer, Vector3 origin, Vector3 destination)
		{
			try
			{
				StrafeProjectile skyfaller = (StrafeProjectile)ThingMaker.MakeThing(StrafeProjectile.StrafeProjectileDef);
				skyfaller.aerialVehicle = strafer;
				skyfaller.origin = origin;
				skyfaller.destination = destination;
				skyfaller.projectileDef = projectileDef;
				return skyfaller;
			}
			catch (Exception ex)
			{
				Log.Error($"Unable to generate StrafeProjectile with projectile <type>{projectileDef.thingClass}</type>. Exception=\"{ex.Message}\"");
			}
			return null;
		}
	}
}
