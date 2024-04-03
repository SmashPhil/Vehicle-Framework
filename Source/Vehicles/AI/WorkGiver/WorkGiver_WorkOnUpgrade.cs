using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;
using System;

namespace Vehicles
{
	public class WorkGiver_WorkOnUpgrade : VehicleWorkGiver
	{
		public override PathEndMode PathEndMode => PathEndMode.Touch;

		public override JobDef JobDef => JobDefOf_Vehicles.UpgradeVehicle;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.GetCachedMapComponent<VehicleReservationManager>().VehicleListers(ReservationType.Upgrade);

		public override bool CanBeWorkedOn(VehiclePawn vehicle)
		{
			return vehicle.CompUpgradeTree != null && vehicle.CompUpgradeTree.Upgrading && vehicle.CompUpgradeTree.StoredCostSatisfied;
		}
	}
}
