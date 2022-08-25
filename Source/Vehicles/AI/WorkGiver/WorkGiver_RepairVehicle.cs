using System;
using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class WorkGiver_RepairVehicle : VehicleWorkGiver
	{
		public override JobDef JobDef => JobDefOf_Vehicles.RepairVehicle;

		public override Predicate<VehiclePawn> VehicleCondition => (vehicle) => vehicle.statHandler.NeedsRepairs;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.GetCachedMapComponent<ListerVehiclesRepairable>().RepairsForFaction(pawn.Faction);
	}
}
