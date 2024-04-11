using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Vehicles
{
	public class WorkGiver_PaintVehicle : VehicleWorkGiver
	{
		public override JobDef JobDef => JobDefOf_Vehicles.PaintVehicle;

		public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn) => pawn.Map.mapPawns.AllPawnsSpawned.Where(pawn => pawn is VehiclePawn vehicle && vehicle.CanPaintNow);

		public override bool CanBeWorkedOn(VehiclePawn vehicle)
		{
			return vehicle.CanPaintNow;
		}
	}
}
