using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class Verb_ShootWorldRecoiled : Verb_ShootRecoiled
	{
		protected GlobalTargetInfo target = GlobalTargetInfo.Invalid;

		public bool IsTargeting => target.IsValid;

		protected float heading;

		public void ResetWorldTarget()
		{
			target = GlobalTargetInfo.Invalid;
		}

		public virtual bool CanHitTarget(GlobalTargetInfo target)
		{
			return caster != null && caster.Spawned && !ApparelPreventsShooting(caster.Position, currentTarget);
		}

		public virtual bool TryStartCastOn(GlobalTargetInfo castTarg, float heading, bool surpriseAttack = false, bool canHitNonTargetPawns = true)
		{
			if (caster is null)
			{
				Log.Error("Verb " + GetUniqueLoadID() + " needs caster to work (possibly lost during saving/loading).", false);
				return false;
			}
			if (!caster.Spawned)
			{
				return false;
			}
			if (state == VerbState.Bursting || !CanHitTarget(castTarg))
			{
				return false;
			}
			this.heading = heading;
			IntVec3 exitTarget = caster.Position.CellFromDistAngle(Building_Artillery.MaxMapDistance, this.heading);
			if (CausesTimeSlowdown(castTarg))
			{
				Find.TickManager.slower.SignalForceNormalSpeed();
			}
			this.surpriseAttack = surpriseAttack;
			canHitNonTargetPawnsNow = canHitNonTargetPawns;
			target = castTarg;
			if (CasterIsPawn && verbProps.warmupTime > 0f)
			{
				CasterPawn.Drawer.Notify_WarmingCastAlongLine(new ShootLine(caster.Position, exitTarget), caster.Position);
				float statValue = CasterPawn.GetStatValue(StatDefOf.AimingDelayFactor, true);
				int ticks = (verbProps.warmupTime * statValue).SecondsToTicks();
				CasterPawn.stances.SetStance(new Stance_Warmup(ticks, exitTarget, this));
			}
			else
			{
				WarmupComplete();
			}
			return true;
		}

		protected override (bool success, Vector3 launchPos, float angle) TryCastShotInternal()
		{
			IntVec3 exitTarget = caster.Position.CellFromDistAngle(Building_Artillery.MaxMapDistance, heading);
			if (!target.HasWorldObject && !target.HasThing)
			{
				return (false, Vector3.zero, 0);
			}
			ThingDef projectile = Projectile;
			if (projectile is null)
			{
				return (false, Vector3.zero, 0);
			}
			bool flag = TryFindShootLineFromTo(caster.Position, exitTarget, out ShootLine shootLine);
			if (verbProps.stopBurstWithoutLos && !flag)
			{
				return (false, Vector3.zero, 0);
			}
			if (EquipmentSource != null)
			{
				CompChangeableProjectile comp = EquipmentSource.GetComp<CompChangeableProjectile>();
				if (comp != null)
				{
					comp.Notify_ProjectileLaunched();
				}
				CompReloadable comp2 = EquipmentSource.GetComp<CompReloadable>();
				if (comp2 != null)
				{
					comp2.UsedOnce();
				}
			}
			Thing launcher = caster;
			Thing equipment = EquipmentSource;
			CompMannable compMannable = caster.TryGetComp<CompMannable>();
			if (compMannable != null && compMannable.ManningPawn != null)
			{
				launcher = compMannable.ManningPawn;
				equipment = caster;
			}
			Vector3 launchPos = caster.DrawPos;
			Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, caster.Position, caster.Map, WipeMode.Vanish);
			if (projectile2.AllComps.NullOrEmpty())
			{
				AccessTools.Field(typeof(ThingWithComps), "comps").SetValue(projectile2, new List<ThingComp>());
			}
			
			projectile2.AllComps.Add(new CompProjectileExitMap(CasterTWC)
			{
				airDefenseDef = AntiAircraftDefOf.FlakProjectile,
				target = target.WorldObject as AerialVehicleInFlight,
				spawnPos = Building_Artillery.RandomWorldPosition(caster.Map.Tile, 1).FirstOrDefault()
			});
			if (caster.def.GetModExtension<ProjectilePropertiesDefModExtension>() is ProjectilePropertiesDefModExtension projectileProps)
			{
				projectile2.AllComps.Add(new CompTurretProjectileProperties(CasterTWC)
				{
					speed = projectileProps.speed > 0 ? projectileProps.speed : projectile2.def.projectile.speed,
					hitflag = ProjectileHitFlags.IntendedTarget,
					hitflags = null
				});
			}
			ThrowDebugText("ToHit" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
			launchPos += new Vector3(VerbProps.shootOffset.x, 0, VerbProps.shootOffset.y).RotatedBy(heading);
			projectile2.Launch(launcher, launchPos, exitTarget, exitTarget, ProjectileHitFlags.IntendedTarget, equipment);
			ThrowDebugText("Hit\nDest", shootLine.Dest);
			return (true, launchPos, heading);
		}
	}
}
