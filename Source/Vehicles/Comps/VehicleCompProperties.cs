using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace Vehicles
{
	public abstract class VehicleCompProperties : CompProperties
	{
		public virtual IEnumerable<VehicleStatCategoryDef> StatCategoryDefs()
		{
			yield break;
		}
	}
}
