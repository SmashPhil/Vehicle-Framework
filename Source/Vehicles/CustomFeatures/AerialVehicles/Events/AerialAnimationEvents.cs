using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public static class AerialAnimationEvents
	{
		public static void ShakeCamera(LaunchProtocol launchProtocol, float magnitude)
		{
			Find.CameraDriver.shaker.DoShake(magnitude);
		}

		public static void Explode(LaunchProtocol launchProtocol, float radius, bool ignoreVehicle = true)
		{
			if (launchProtocol.Position.IsValid)
			{
				List<Thing> ignoreThings = ignoreVehicle ? new List<Thing>() { launchProtocol.Vehicle } : null;
				GenExplosion.DoExplosion(launchProtocol.Position, launchProtocol.Map, radius, DamageDefOf.Bomb, launchProtocol.Vehicle, ignoredThings: ignoreThings);
			}
		}
	}
}
