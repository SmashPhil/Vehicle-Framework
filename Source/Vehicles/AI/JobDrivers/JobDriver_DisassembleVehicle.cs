using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;

namespace Vehicles
{
	public class JobDriver_DisassembleVehicle : JobDriver
	{
		protected const float MinDeconstructWork = 50f;
		protected const float MaxDeconstructWork = 5000f;

		protected float workLeft;

		protected float totalNeededWork;

		protected VehiclePawn Vehicle => job.targetA.Thing as VehiclePawn;

		protected virtual DesignationDef Designation => DesignationDefOf.Deconstruct;

		protected virtual float TotalWork
		{
			get
			{
				return Mathf.Clamp(Vehicle.VehicleDef.buildDef.GetStatValueAbstract(StatDefOf.WorkToBuild, Vehicle.Stuff), MinDeconstructWork, MaxDeconstructWork);
			}
		}

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return pawn.Reserve(Vehicle, job);
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOn(() => Vehicle is null || !Vehicle.DeconstructibleBy(pawn.Faction));
			this.FailOnThingMissingDesignation(TargetIndex.A, Designation);
			this.FailOnForbidden(TargetIndex.A);
			yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
			Toil disassembleVehicle = new Toil().FailOnDestroyedNullOrForbidden(TargetIndex.A);
			disassembleVehicle.initAction = delegate ()
			{
				totalNeededWork = TotalWork;
				workLeft = totalNeededWork;
			};
			disassembleVehicle.tickAction = delegate ()
			{
				workLeft -= pawn.GetStatValue(StatDefOf.ConstructionSpeed) * 1.7f;
				TickAction();
				if (workLeft <= 0)
				{
					disassembleVehicle.actor.jobs.curDriver.ReadyForNextToil();
				}
			};
			disassembleVehicle.FailOnMoving(TargetIndex.A);
			disassembleVehicle.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
			disassembleVehicle.defaultCompleteMode = ToilCompleteMode.Never;
			disassembleVehicle.WithEffect(Vehicle.def.repairEffect, TargetIndex.A);
			disassembleVehicle.WithProgressBar(TargetIndex.A, () => 1 - workLeft / totalNeededWork);
			disassembleVehicle.activeSkill = () => SkillDefOf.Construction;
			yield return disassembleVehicle;
			yield return new Toil
			{
				initAction = delegate ()
				{
					FinishedRemoving();
					Map.designationManager.RemoveAllDesignationsOn(Vehicle);
				},
				defaultCompleteMode = ToilCompleteMode.Instant
			};
		}

		protected virtual void FinishedRemoving()
		{
			Vehicle.Destroy(DestroyMode.Deconstruct);
			pawn.records.Increment(RecordDefOf.ThingsDeconstructed);
		}

		protected virtual void TickAction()
		{
			if (Vehicle.VehicleDef.buildDef.CostListAdjusted(Vehicle.Stuff, true).Count > 0)
			{
				pawn.skills.Learn(SkillDefOf.Construction, 0.25f, false);
			}
		}
	}
}
