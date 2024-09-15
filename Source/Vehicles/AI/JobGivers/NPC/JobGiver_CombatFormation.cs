using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using UnityEngine;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public abstract class JobGiver_CombatFormation : ThinkNode_JobGiver
	{
		protected static readonly Action<Pawn_MindState> Notify_EngagedTarget;

		protected bool humanlikesOnly = true;
		protected bool ignoreNonCombatants = false;

		static JobGiver_CombatFormation()
		{
			MethodInfo engagedTargetMethod = AccessTools.Method(typeof(Pawn_MindState), "Notify_EngagedTarget");
			Notify_EngagedTarget = (Action<Pawn_MindState>)Delegate.CreateDelegate(typeof(Action<Pawn_MindState>), engagedTargetMethod);
		}

		protected virtual IntRange ExpiryInterval => new IntRange(30, 30);

		protected virtual int TicksSinceEngageToLoseTarget => 400;

		protected virtual bool OnlyUseRanged => true;

		// Position to post up for support by fire
		protected abstract bool TryFindCombatPosition(VehiclePawn vehicle, out IntVec3 dest);

		protected virtual float TargetAcquireRadius(VehiclePawn vehicle) => 56;

		protected virtual bool CanRam(VehiclePawn vehicle) => false;

		// How far vehicle can wander from escortee / defense point
		protected virtual float GetFlagRadius(VehiclePawn vehicle)
		{
			return 999999f;
		}

		protected virtual IntVec3 GetFlagPosition(VehiclePawn vehicle)
		{
			return IntVec3.Invalid;
		}

		protected virtual bool ExtraTargetValidator(VehiclePawn vehicle, Thing target)
		{
			return target.Faction != Faction.OfPlayer && (!humanlikesOnly || target is not Pawn pawn || pawn.RaceProps.Humanlike);
		}

		public override ThinkNode DeepCopy(bool resolve = true)
		{
			JobGiver_CombatFormation jobGiver = (JobGiver_CombatFormation)base.DeepCopy(resolve);
			jobGiver.humanlikesOnly = humanlikesOnly;
			jobGiver.ignoreNonCombatants = ignoreNonCombatants;
			return jobGiver;
		}

		protected override Job TryGiveJob(Pawn pawn)
		{
			if (pawn is not VehiclePawn vehicle)
			{
				Log.Error($"Trying to assign vehicle job to non-vehicle pawn. This is not allowed!");
				return null;
			}
			UpdateEnemyTarget(vehicle);
			if (vehicle.mindState.enemyTarget is not Thing enemyTarget)
			{
				return null;
			}
			if (enemyTarget is Pawn targetPawn && targetPawn.IsPsychologicallyInvisible())
			{
				return null;
			}
			if (OnlyUseRanged)
			{
				if (!TryFindCombatPosition(vehicle, out IntVec3 intVec))
				{
					return null;
				}
				if (intVec == vehicle.Position)
				{
					return JobMaker.MakeJob(JobDefOf_Vehicles.IdleVehicle, ExpiryInterval.RandomInRange, true);
				}
				Job job = JobMaker.MakeJob(JobDefOf.Goto, intVec);
				job.expiryInterval = ExpiryInterval.RandomInRange;
				job.checkOverrideOnExpire = true;
				return job;
			}
			// TODO - Add special case for how vehicles should handle pawns being within melee range
			// TODO - Add ramming capability
			return null;
		}

		protected virtual bool ShouldLoseTarget(VehiclePawn vehicle)
		{
			Thing enemyTarget = vehicle.mindState.enemyTarget;
			float keepRadiusSqrd = Mathf.Pow(vehicle.VehicleDef.npcProperties.targetKeepRadius, 2);
			if (!enemyTarget.Destroyed && Find.TickManager.TicksGame - vehicle.mindState.lastEngageTargetTick <= TicksSinceEngageToLoseTarget &&
				vehicle.CanReachVehicle(enemyTarget, PathEndMode.Touch, Danger.Deadly, TraverseMode.ByPawn) && 
				(vehicle.Position - enemyTarget.Position).LengthHorizontalSquared <= keepRadiusSqrd)
			{
				return enemyTarget is IAttackTarget attackTarget && attackTarget.ThreatDisabled(vehicle);
			}
			return true;
		}

		protected abstract void UpdateEnemyTarget(VehiclePawn vehicle);
	}
}
