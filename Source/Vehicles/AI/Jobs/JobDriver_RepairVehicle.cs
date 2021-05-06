using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;
using Vehicles.Defs;

namespace Vehicles
{
	public class JobDriver_RepairVehicle : JobDriver
	{
		private const float TicksForRepair = 100f;

		protected float ticksToNextRepair;

		public VehiclePawn Vehicle
		{
			get
			{
				if(job.targetA.Thing is VehiclePawn vehicle)
				{
					return vehicle;
				}
				Log.Error("Attempting to repair non-vehicle pawn.");
				return null;
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			int maxWorkers = Vehicle.TotalAllowedFor(JobDefOf_Vehicles.UpgradeVehicle);
			LocalTargetInfo target = Vehicle.SurroundingCells.FirstOrDefault(c => pawn.Map.GetCachedMapComponent<VehicleReservationManager>().CanReserve<LocalTargetInfo, VehicleTargetReservation>(Vehicle, pawn, c));
			return target.IsValid && pawn.Map.GetCachedMapComponent<VehicleReservationManager>().Reserve<LocalTargetInfo, VehicleTargetReservation>(Vehicle, pawn, job, target, maxWorkers);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
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
				actor.skills.Learn(SkillDefOf.Construction, 0.08f, false);
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
					component.HealComponent(SettingsCache.TryGetValue(Vehicle.VehicleDef, typeof(VehicleDef), "repairRate", Vehicle.VehicleDef.repairRate));
				}
			};
			repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			repair.WithEffect(TargetThingA.def.repairEffect, TargetIndex.A);
			repair.defaultCompleteMode = ToilCompleteMode.Delay;
			repair.defaultDuration = 2000;
			repair.activeSkill = (() => SkillDefOf.Construction);
			yield return repair;
		}
	}
}
