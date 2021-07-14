using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;
using SmashTools;

namespace Vehicles
{
	public class Verb_ShootRealistic : Verb_Shoot
	{
		public ThingWithComps CasterTWC => caster as ThingWithComps;

		public VerbProperties_Recoil VerbProps => verbProps as VerbProperties_Recoil;

		protected void ThrowDebugText(string text)
		{
			if (DebugViewSettings.drawShooting)
			{
				MoteMaker.ThrowText(caster.DrawPos, caster.Map, text, -1f);
			}
		}

		protected void ThrowDebugText(string text, IntVec3 c)
		{
			if (DebugViewSettings.drawShooting)
			{
				MoteMaker.ThrowText(c.ToVector3Shifted(), caster.Map, text, -1f);
			}
		}

		protected virtual bool CausesTimeSlowdown(GlobalTargetInfo target) => verbProps.CausesTimeSlowdown;

		protected virtual bool CausesTimeSlowdown(LocalTargetInfo target)
		{
			if (!verbProps.CausesTimeSlowdown)
			{
				return false;
			}
			if (!target.HasThing)
			{
				return false;
			}
			Thing thing = target.Thing;
			if (thing.def.category != ThingCategory.Pawn && (thing.def.building == null || !thing.def.building.IsTurret))
			{
				return false;
			}
			Pawn pawn = thing as Pawn;
			bool flag = pawn != null && pawn.Downed;
			return (thing.Faction == Faction.OfPlayer && caster.HostileTo(Faction.OfPlayer)) || (caster.Faction == Faction.OfPlayer && thing.HostileTo(Faction.OfPlayer) && !flag);
		}

		protected override bool TryCastShot()
		{
			(bool success, Vector3 launchPos, float angle) = TryCastShotInternal();
			if (success)
			{
				InitTurretMotes(launchPos, angle);
			}
			return success;
		}

		protected virtual (bool success, Vector3 launchPos, float angle) TryCastShotInternal()
		{
			if (currentTarget.HasThing && currentTarget.Thing.Map != caster.Map)
			{
				return (false, Vector3.zero, 0);
			}
			ThingDef projectile = Projectile;
			if (projectile == null)
			{
				return (false, Vector3.zero, 0);
			}
			ShootLine shootLine;
			bool flag = TryFindShootLineFromTo(caster.Position, currentTarget, out shootLine);
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
			float angle = launchPos.AngleToPoint(currentTarget.CenterVector3);
			Projectile projectile2 = (Projectile)GenSpawn.Spawn(projectile, shootLine.Source, caster.Map, WipeMode.Vanish);
			if (caster.def.GetModExtension<ProjectilePropertiesDefModExtension>() is ProjectilePropertiesDefModExtension projectileProps)
			{
				projectile2.AllComps.Insert(0, new CompTurretProjectileProperties(CasterTWC)
				{
					speed = projectileProps.speed > 0 ? projectileProps.speed : projectile2.def.projectile.speed,
					hitflag = projectileProps.projectileHitFlag,
					hitflags = projectileProps.hitFlagDef
				});
			}
			if (verbProps.ForcedMissRadius > 0.5f)
			{
				float num = VerbUtility.CalculateAdjustedForcedMiss(verbProps.ForcedMissRadius, currentTarget.Cell - caster.Position);
				if (num > 0.5f)
				{
					int max = GenRadial.NumCellsInRadius(num);
					int num2 = Rand.Range(0, max);
					if (num2 > 0)
					{
						IntVec3 c = currentTarget.Cell + GenRadial.RadialPattern[num2];
						launchPos += new Vector3(VerbProps.shootOffset.x, 0, VerbProps.shootOffset.y).RotatedBy(angle);
						ThrowDebugText("ToRadius");
						ThrowDebugText("Rad\nDest", c);
						ProjectileHitFlags projectileHitFlags = ProjectileHitFlags.NonTargetWorld;
						if (Rand.Chance(0.5f))
						{
							projectileHitFlags = ProjectileHitFlags.All;
						}
						if (!canHitNonTargetPawnsNow)
						{
							projectileHitFlags &= ~ProjectileHitFlags.NonTargetPawns;
						}
						projectile2.Launch(launcher, launchPos, c, currentTarget, projectileHitFlags, false, equipment);
						return (true, launchPos, angle);
					}
				}
			}
			ShotReport shotReport = ShotReport.HitReportFor(caster, this, currentTarget);
			Thing randomCoverToMissInto = shotReport.GetRandomCoverToMissInto();
			ThingDef targetCoverDef = (randomCoverToMissInto != null) ? randomCoverToMissInto.def : null;
			if (!Rand.Chance(shotReport.AimOnTargetChance_IgnoringPosture))
			{
				shootLine.ChangeDestToMissWild(shotReport.AimOnTargetChance_StandardTarget);
				ThrowDebugText("ToWild" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
				ThrowDebugText("Wild\nDest", shootLine.Dest);
				ProjectileHitFlags projectileHitFlags2 = ProjectileHitFlags.NonTargetWorld;
				if (Rand.Chance(0.5f) && canHitNonTargetPawnsNow)
				{
					projectileHitFlags2 |= ProjectileHitFlags.NonTargetPawns;
				}
				launchPos += new Vector3(VerbProps.shootOffset.x, 0, VerbProps.shootOffset.y).RotatedBy(angle);
				projectile2.Launch(launcher, launchPos, shootLine.Dest, currentTarget, projectileHitFlags2, false, equipment, targetCoverDef);
				return (true, launchPos, angle);
			}
			if (currentTarget.Thing != null && currentTarget.Thing.def.category == ThingCategory.Pawn && !Rand.Chance(shotReport.PassCoverChance))
			{
				ThrowDebugText("ToCover" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));
				ThrowDebugText("Cover\nDest", randomCoverToMissInto.Position);
				ProjectileHitFlags projectileHitFlags3 = ProjectileHitFlags.NonTargetWorld;
				if (canHitNonTargetPawnsNow)
				{
					projectileHitFlags3 |= ProjectileHitFlags.NonTargetPawns;
				}
				launchPos += new Vector3(VerbProps.shootOffset.x, 0, VerbProps.shootOffset.y).RotatedBy(angle);
				projectile2.Launch(launcher, launchPos, randomCoverToMissInto, currentTarget, projectileHitFlags3, false, equipment, targetCoverDef);
				return (true, launchPos, angle);
			}
			ProjectileHitFlags projectileHitFlags4 = ProjectileHitFlags.IntendedTarget;
			if (canHitNonTargetPawnsNow)
			{
				projectileHitFlags4 |= ProjectileHitFlags.NonTargetPawns;
			}
			if (!currentTarget.HasThing || currentTarget.Thing.def.Fillage == FillCategory.Full)
			{
				projectileHitFlags4 |= ProjectileHitFlags.NonTargetWorld;
			}
			ThrowDebugText("ToHit" + (canHitNonTargetPawnsNow ? "\nchntp" : ""));

			if (currentTarget.Thing != null)
			{
				angle = launchPos.AngleToPoint(currentTarget.CenterVector3);
				launchPos += new Vector3(VerbProps.shootOffset.x, 0, VerbProps.shootOffset.y).RotatedBy(angle);
				projectile2.Launch(launcher, launchPos, currentTarget, currentTarget, projectileHitFlags4, false, equipment, targetCoverDef);
				ThrowDebugText("Hit\nDest", currentTarget.Cell);
			}
			else
			{
				angle = launchPos.AngleToPoint(shootLine.Dest.ToVector3Shifted());
				launchPos += new Vector3(VerbProps.shootOffset.x, 0, VerbProps.shootOffset.y).RotatedBy(angle);
				projectile2.Launch(launcher, launchPos, shootLine.Dest, currentTarget, projectileHitFlags4, false, equipment, targetCoverDef);
				ThrowDebugText("Hit\nDest", shootLine.Dest);
			}
			return (true, launchPos, angle);
		}

		public virtual void InitTurretMotes(Vector3 loc, float angle)
		{
			if (!VerbProps.motes.NullOrEmpty())
			{
				foreach (AnimationProperties moteProps in VerbProps.motes)
				{
					Vector3 moteLoc = loc;
					if (loc.ShouldSpawnMotesAt(caster.Map))
					{
						try
						{
							float altitudeLayer = Altitudes.AltitudeFor(moteProps.moteDef.altitudeLayer);
							moteLoc += new Vector3(VerbProps.shootOffset.x + moteProps.offset.x, altitudeLayer + moteProps.offset.y, VerbProps.shootOffset.y + moteProps.offset.z).RotatedBy(angle);
							Mote mote = (Mote)ThingMaker.MakeThing(moteProps.moteDef);
							mote.exactPosition = moteLoc;
							mote.exactRotation = moteProps.exactRotation.RandomInRange;
							mote.instanceColor = moteProps.color;
							mote.rotationRate = moteProps.rotationRate;
							mote.Scale = moteProps.scale;
							if (mote is MoteThrown thrownMote)
							{
								float thrownAngle = angle + moteProps.angleThrown.RandomInRange;
								thrownMote.SetVelocity(thrownAngle, moteProps.speedThrown.RandomInRange);
								if (thrownMote is MoteThrownExpand expandMote)
								{
									if (expandMote is MoteThrownSlowToSpeed accelMote)
									{
										accelMote.SetDecelerationRate(moteProps.deceleration.RandomInRange, moteProps.fixedAcceleration, thrownAngle);
									}
									expandMote.growthRate = moteProps.growthRate.RandomInRange;
								}
							}
							if (mote is Mote_CannonPlume cannonMote)
							{
								cannonMote.cyclesLeft = moteProps.cycles;
								cannonMote.animationType = moteProps.animationType;
								cannonMote.angle = angle;
							}
							mote.def = moteProps.moteDef;
							mote.PostMake();
							GenSpawn.Spawn(mote, moteLoc.ToIntVec3(), caster.Map, WipeMode.Vanish);
						}
						catch (Exception ex)
						{
							SmashLog.Error($"Failed to spawn mote at {loc}. MoteDef = <field>{moteProps.moteDef?.defName ?? "Null"}</field> Exception = {ex.Message}");
						}
					}
				}
			}
		}
	}
}
