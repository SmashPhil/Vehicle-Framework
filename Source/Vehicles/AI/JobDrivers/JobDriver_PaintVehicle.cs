using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class JobDriver_PaintVehicle : VehicleJobDriver
	{
		protected const float WorkPerCell = 200f;

		protected virtual float TotalWork => WorkPerCell * (Vehicle.VehicleDef.Size.x * Vehicle.VehicleDef.Size.z);

		protected override JobDef JobDef => JobDefOf_Vehicles.PaintVehicle;

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
			Toil gotoCell = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
			gotoCell.FailOnMoving(TargetIndex.A);
			yield return gotoCell;
			Toil paintVehicle = new Toil();
			paintVehicle.initAction = delegate ()
			{
				Vehicle.sharedJob.JobStarted(JobDef, pawn);
				GenClamor.DoClamor(paintVehicle.actor, 5, VehicleClamorDefOf.VF_Painting);
			};
			paintVehicle.tickAction = delegate ()
			{
				float statValue = paintVehicle.actor.GetStatValue(StatDefOf.GeneralLaborSpeed, true);
				Vehicle.sharedJob.workDone += statValue;
				if (Vehicle.sharedJob.workDone >= TotalWork)
				{
					Vehicle.SetColor();
					paintVehicle.actor.jobs.EndCurrentJob(JobCondition.Succeeded);
				}
			};
			paintVehicle.FailOnMoving(TargetIndex.A);
			paintVehicle.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			paintVehicle.WithEffect(Vehicle.def.repairEffect, TargetIndex.A);
			paintVehicle.WithProgressBar(TargetIndex.A, () => Vehicle.sharedJob.workDone / TotalWork);
			paintVehicle.defaultCompleteMode = ToilCompleteMode.Never;
			paintVehicle.activeSkill = () => SkillDefOf.Construction;
			paintVehicle.AddFinishAction(() => Vehicle.sharedJob.JobEnded(pawn));
			yield return paintVehicle;
		}
	}
}
