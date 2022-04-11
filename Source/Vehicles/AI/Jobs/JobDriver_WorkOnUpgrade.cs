using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public class JobDriver_WorkOnUpgrade : JobDriver
	{
		public VehiclePawn Vehicle
		{
			get
			{
				if (job.targetA.Thing is VehiclePawn vehicle)
				{
					return vehicle;
				}
				Log.Error($"Cannot work on upgrade. TargetA: {TargetA.Thing.LabelShortCap} should be a vehicle");
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
			Toil work = new Toil();
			work.tickAction = delegate ()
			{
				Pawn actor = work.actor;
				if (!Vehicle.CompUpgradeTree.CurrentlyUpgrading)
				{
					actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
					return;
				}
				actor.skills.Learn(SkillDefOf.Construction, 0.08f, false);
				float statValue = actor.GetStatValue(StatDefOf.ConstructionSpeed, true);
				if (statValue < 1)
				{
					statValue = 1;
				}
				Vehicle.CompUpgradeTree.NodeUnlocking.WorkLeft -= statValue;
			};
			work.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			work.WithEffect(TargetThingA.def.repairEffect, TargetIndex.A);
			work.defaultCompleteMode = ToilCompleteMode.Never;
			work.activeSkill = () => SkillDefOf.Construction;
			yield return work;
		}
	}
}
