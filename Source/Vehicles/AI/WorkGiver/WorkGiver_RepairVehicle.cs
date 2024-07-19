using System;
using System.Collections.Generic;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class WorkGiver_RepairVehicle : VehicleWorkGiver
	{
		public override JobDef JobDef => JobDefOf_Vehicles.RepairVehicle;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.GetCachedMapComponent<ListerVehiclesRepairable>().RepairsForFaction(pawn.Faction);

		public override Danger MaxPathDanger(Pawn pawn)
		{
			return Danger.Deadly;
		}

		public override bool CanBeWorkedOn(VehiclePawn vehicle)
		{
			return vehicle.statHandler.NeedsRepairs;
		}
	}
}
