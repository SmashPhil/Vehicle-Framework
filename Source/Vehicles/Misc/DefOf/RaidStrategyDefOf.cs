using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;

namespace Vehicles
{
	[DefOf]
	public static class VehicleRaidStrategyDefOf
	{
		static VehicleRaidStrategyDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(VehicleRaidStrategyDefOf));
		}

		public static RaidStrategyDef ArmoredAttack;
	}
}
