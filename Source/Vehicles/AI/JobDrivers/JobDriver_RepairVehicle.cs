using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class JobDriver_RepairVehicle : VehicleJobDriver
	{
		private const float TicksForRepair = 60;

		protected float ticksToNextRepair;

		protected override JobDef JobDef => JobDefOf_Vehicles.RepairVehicle;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			Toil gotoCell = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
			gotoCell.FailOnMoving(TargetIndex.A);
			yield return gotoCell;
			Toil repair = new Toil
			{
				initAction = delegate ()
				{
					ticksToNextRepair = TicksForRepair;
				}
			};
			repair.tickAction = delegate ()
			{
				Pawn actor = repair.actor;
				actor.skills?.Learn(SkillDefOf.Construction, 0.08f, false);
				float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
				ticksToNextRepair -= statValue;
				if (ticksToNextRepair <= 0f)
				{
					if (!Vehicle.statHandler.ComponentsPrioritized.Any(c => c.HealthPercent < 1))
					{
						Vehicle.Map.GetCachedMapComponent<ListerVehiclesRepairable>().Notify_VehicleRepaired(Vehicle);
						actor.records.Increment(RecordDefOf.ThingsRepaired);
						actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
						return;
					}
					ticksToNextRepair = TicksForRepair;
					var component = Vehicle.statHandler.ComponentsPrioritized.FirstOrDefault(c => c.HealthPercent < 1);
					component.HealComponent(Vehicle.GetStatValue(VehicleStatDefOf.RepairRate));
					Vehicle.CrashLanded = false;
				}
			};
			repair.FailOnMoving(TargetIndex.A);
			repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			repair.WithEffect(Vehicle.def.repairEffect, TargetIndex.A);
			repair.defaultCompleteMode = ToilCompleteMode.Delay;
			repair.defaultDuration = 2000;
			repair.activeSkill = () => SkillDefOf.Construction;
			yield return repair;
		}
	}
}
