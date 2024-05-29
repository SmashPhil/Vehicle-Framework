using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class JobDriver_RepairVehicle : JobDriver_WorkVehicle
	{
		public const float TicksForRepair = 60;

		protected override JobDef JobDef => JobDefOf_Vehicles.RepairVehicle;

		protected override float TotalWork => TicksForRepair;

		protected override StatDef Stat => StatDefOf.ConstructionSpeed;

		protected override void WorkComplete(Pawn actor)
		{
			if (!Vehicle.statHandler.ComponentsPrioritized.Any(c => c.HealthPercent < 1))
			{
				MapComponentCache<ListerVehiclesRepairable>.GetComponent(Vehicle.Map).Notify_VehicleRepaired(Vehicle);
				actor.records.Increment(RecordDefOf.ThingsRepaired);
				actor.jobs.EndCurrentJob(JobCondition.Succeeded);
				return;
			}
			ResetWork();
			VehicleComponent component = Vehicle.statHandler.ComponentsPrioritized.FirstOrDefault(c => c.HealthPercent < 1);
			component.HealComponent(Vehicle.GetStatValue(VehicleStatDefOf.RepairRate));
			Vehicle.CrashLanded = false;
		}
	}
}
