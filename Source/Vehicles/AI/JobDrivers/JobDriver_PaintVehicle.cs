using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicles
{
	public class JobDriver_PaintVehicle : JobDriver
	{
		protected const float WorkPerCell = 150f;

		protected float workDone;

		protected VehiclePawn Vehicle => TargetA.Thing as VehiclePawn;

		protected virtual float TotalWork => WorkPerCell * (Vehicle.VehicleDef.Size.x * Vehicle.VehicleDef.Size.z);

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(job.GetTarget(TargetIndex.A), job);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedOrNull(TargetIndex.A);
			Toil paintVehicle = new Toil();
			paintVehicle.initAction = delegate ()
			{
				GenClamor.DoClamor(paintVehicle.actor, 5, VehicleClamorDefOf.VF_Painting);
			};
			paintVehicle.tickAction = delegate ()
			{
				float statValue = paintVehicle.actor.GetStatValue(StatDefOf.GeneralLaborSpeed, true);
				workDone += statValue;
				if (workDone >= TotalWork)
				{
					Vehicle.SetColor();
					paintVehicle.actor.jobs.EndCurrentJob(JobCondition.Succeeded);
				}
			};
			paintVehicle.AddFinishAction(delegate ()
			{
				if (Vehicle.vPather.Moving)
				{
					pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true, true);
				}
			});
			paintVehicle.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			paintVehicle.WithProgressBar(TargetIndex.A, () => workDone / TotalWork);
			paintVehicle.defaultCompleteMode = ToilCompleteMode.Never;
			paintVehicle.activeSkill = () => SkillDefOf.Construction;
			yield return paintVehicle;
		}

		public override void ExposeData()
		{
			Scribe_Values.Look(ref workDone, nameof(workDone));
		}
	}
}
