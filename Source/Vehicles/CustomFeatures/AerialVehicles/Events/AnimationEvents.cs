using SmashTools.Animations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Vehicles
{
	public static class AnimationEvents
	{
		//For New Animation System
		[AnimationEvent]
		private static void ShakeCamera(float magnitude)
		{
			Find.CameraDriver.shaker.DoShake(magnitude);
		}

		[AnimationEvent]
		private static void Explode(IAnimator __animator, DamageDef damageDef, float radius, bool damageThing = false)
		{
			if (__animator is Thing thing && thing.Spawned)
			{
				List<Thing> ignoreThings = damageThing ? null : new List<Thing>() { thing };
				GenExplosion.DoExplosion(thing.Position, thing.Map, radius, damageDef, thing, ignoredThings: ignoreThings);
			}
		}
	}
}
