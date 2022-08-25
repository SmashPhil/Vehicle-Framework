using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicles
{
	public class WorkGiver_DisassembleVehicle : WorkGiver_RemoveBuilding
	{
		protected override JobDef RemoveBuildingJob => JobDefOf_Vehicles.DisassembleVehicle;

		protected override DesignationDef Designation => DesignationDefOf.Deconstruct;

		public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			return JobMaker.MakeJob(RemoveBuildingJob, t);
		}

		public override bool HasJobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			return thing is VehiclePawn vehicle && vehicle.DeconstructibleBy(pawn.Faction) && pawn.CanReserve(vehicle) && pawn.Map.designationManager.DesignationOn(vehicle, Designation) != null;
		}
	}
}
