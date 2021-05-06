using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public class CompTurretProjectileProperties : ThingComp
	{
		public float speed = -1;
		public ProjectileHitFlags? hitflag;
		public CustomHitFlags hitflags;

		public CompTurretProjectileProperties(VehiclePawn vehicle, VehicleTurretDef turretDef, Projectile projectile)
		{
			parent = vehicle;
			speed = turretDef.projectileSpeed > 0 ? turretDef.projectileSpeed : projectile.def.projectile.speed;
			hitflag = turretDef.hitFlags;
			hitflags = turretDef.attachProjectileFlag;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref speed, "speed");
			Scribe_Values.Look(ref hitflag, "hitflag");
			Scribe_Defs.Look(ref hitflags, "hitflags");
		}
	}
}
