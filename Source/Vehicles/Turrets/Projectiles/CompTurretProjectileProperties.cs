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
		
		public CompTurretProjectileProperties(ThingWithComps parent)
		{
			this.parent = parent;
		}

		public override void PostExposeData()
		{
			base.PostExposeData();
			Scribe_Values.Look(ref speed, nameof(speed));
			Scribe_Values.Look(ref hitflag, nameof(hitflag));
			Scribe_Defs.Look(ref hitflags, nameof(hitflags));
		}
	}
}
