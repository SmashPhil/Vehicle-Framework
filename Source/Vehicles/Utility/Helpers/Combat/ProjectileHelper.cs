using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;
using UnityEngine;

namespace Vehicles
{
	public static class ProjectileHelper
	{
		private static LinearCurve distanceBySpeed = new LinearCurve()
		{
			new CurvePoint(0, 3),
			new CurvePoint(10, 6),
			new CurvePoint(60, 10),
			new CurvePoint(100, 20),
			new CurvePoint(200, 30),
			new CurvePoint(400, 50),
		};

		public static bool DeflectProjectile(Projectile projectile, VehiclePawn deflectedOff)
		{
			SoundDefOf.MetalHitImportant.PlayOneShot(deflectedOff);
			float angle = (projectile.ExactRotation.eulerAngles.y + Rand.Range(-10, 10)).ClampAndWrap(0, 360);
			
			//CompTurretProjectileProperties projectileComp = projectile.TryGetComp<CompTurretProjectileProperties>();
			//if (projectileComp == null)
			//{
			//	projectileComp = new CompTurretProjectileProperties(projectile);
			//	projectile.TryAddComp(projectileComp);
			//}

			float speed = projectile.def.projectile.speed;
			//if (projectileComp?.speed > 0)
			//{
			//	speed = projectileComp.speed;
			//}
			float distance = distanceBySpeed.Evaluate(speed);
			FloatRange distanceRange = new FloatRange(distance * 0.9f, distance * 1.1f);
			
			Projectile_Deflected deflectedProjectile = new Projectile_Deflected();
			deflectedProjectile.def = projectile.def;
			deflectedProjectile.SetStuffDirect(projectile.Stuff);
			deflectedProjectile.PostMake();
			deflectedProjectile.PostPostMake();
			
			deflectedProjectile.Deflect(projectile, deflectedOff, distanceRange.RandomInRange, angle, Mathf.Sqrt(distance));

			return true;
		}
	}
}
