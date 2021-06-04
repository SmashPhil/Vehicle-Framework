using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public class ProjectilePropertiesDefModExtension : DefModExtension
	{
		public float speed = -1;
		public ProjectileHitFlags? projectileHitFlag;
		public CustomHitFlags hitFlagDef;
	}
}
