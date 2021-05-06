using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	public class WorldObjectCompProperties_SettlementAirDefense : WorldObjectCompProperties
	{
		public int defenseRadius;

		public WorldObjectCompProperties_SettlementAirDefense()
		{
			compClass = typeof(SettlementAirDefense);
		}
	}
}
