using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using SmashTools;

namespace Vehicles
{
	public abstract class JobDriver_WorkVehicle : VehicleJobDriver
	{
		protected abstract float TotalWork { get; }

		protected virtual float Work { get; set; }

		protected abstract StatDef Stat { get; }

		protected virtual SkillDef Skill => null;

		protected virtual float SkillAmount => 0.08f;

		protected virtual ToilCompleteMode ToilCompleteMode => ToilCompleteMode.Delay;

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
					ResetWork();
				}
			};
			repair.tickAction = delegate ()
			{
				Pawn actor = repair.actor;
				if (Skill != null)
				{
					actor.skills?.Learn(Skill, SkillAmount);
				}
				float statValue = actor.GetStatValue(Stat);
				Work -= statValue;
				if (Work <= 0f)
				{
					WorkComplete(actor);
				}
			};
			repair.FailOnMoving(TargetIndex.A);
			repair.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			repair.WithEffect(Vehicle.def.repairEffect, TargetIndex.A);
			repair.defaultCompleteMode = ToilCompleteMode;
			repair.defaultDuration = 2000;
			if (Skill != null)
			{
				repair.activeSkill = () => Skill;
			}
			yield return repair;
		}

		protected virtual void ResetWork()
		{
			Work = TotalWork;
		}

		protected abstract void WorkComplete(Pawn actor);
	}
}
