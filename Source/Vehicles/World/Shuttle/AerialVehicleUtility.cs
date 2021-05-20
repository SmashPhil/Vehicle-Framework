using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace Vehicles
{
	public static class AerialVehicleUtility
	{
		public static Settlement SettlementVisitedNow(AerialVehicleInFlight aerial)
		{
			if (!aerial.Spawned || aerial.vehicle.CompVehicleLauncher.inFlight)
			{
				return null;
			}
			List<Settlement> settlementBases = Find.WorldObjects.SettlementBases;
			for (int i = 0; i < settlementBases.Count; i++)
			{
				Settlement settlement = settlementBases[i];
				if (settlement.Tile == aerial.flightPath.First.tile && settlement.Faction != aerial.Faction && settlement.Visitable)
				{
					return settlement;
				}
			}
			return null;
		}
	}
}
