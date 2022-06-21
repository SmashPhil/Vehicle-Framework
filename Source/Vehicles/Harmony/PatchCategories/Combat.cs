using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;

namespace Vehicles
{
	internal class Combat : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(Projectile), "StartingTicksToImpact"),
				postfix: new HarmonyMethod(typeof(Combat),
				nameof(StartingTicksFromTurret)));
			VehicleHarmony.Patch(original: AccessTools.PropertyGetter(typeof(Projectile), nameof(Projectile.HitFlags)),
				postfix: new HarmonyMethod(typeof(Combat),
				nameof(OverriddenHitFlags)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Projectile), "CanHit"),
				prefix: new HarmonyMethod(typeof(Combat),
				nameof(TurretHitFlags)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Projectile), "Impact"),
				prefix: new HarmonyMethod(typeof(Combat),
				nameof(RegisterImpactCell)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(DamageWorker), "ExplosionDamageThing"),
				postfix: new HarmonyMethod(typeof(Combat),
				nameof(ExplosionDamageVehicle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Thing), nameof(Thing.Destroy)),
				prefix: new HarmonyMethod(typeof(Combat),
				nameof(ProjectileMapToWorld)));
		}

		public static void StartingTicksFromTurret(Projectile __instance, ref float __result, Vector3 ___origin, Vector3 ___destination)
		{
			if (__instance.AllComps.FirstOrDefault(c => c is CompTurretProjectileProperties) is CompTurretProjectileProperties comp)
			{
				float num = (___origin - ___destination).magnitude / (comp.speed / 100);
				if (num <= 0f)
				{
					num = 0.001f;
				}
				__result = num;
			}
		}

		public static void OverriddenHitFlags(Projectile __instance, ref ProjectileHitFlags __result)
		{
			if (__instance.AllComps.FirstOrDefault(c => c is CompTurretProjectileProperties) is CompTurretProjectileProperties comp && comp.hitflag.HasValue)
			{
				__result = comp.hitflag.Value;
			}
		}

		public static bool TurretHitFlags(Thing thing, Projectile __instance, Thing ___launcher, ref bool __result)
		{
			if (__instance.AllComps.FirstOrDefault(c => c is CompTurretProjectileProperties) is CompTurretProjectileProperties comp)
			{
				if (!thing.Spawned)
				{
					__result = false;
					return false;
				}
				if (thing == ___launcher)
				{
					__result = false;
					return false;
				}
			
				bool flag = false;
				foreach (IntVec3 c in thing.OccupiedRect())
				{
					List<Thing> thingList = c.GetThingList(__instance.Map);
					bool flag2 = false;
					for (int i = 0; i < thingList.Count; i++)
					{
						if (thingList[i] != thing && ((comp.hitflags != null && thingList[i].def.fillPercent >= comp.hitflags.minFillPercent) ||
							(comp.hitflags is null && thingList[i].def.Fillage == FillCategory.Full))&& thingList[i].def.Altitude >= thing.def.Altitude)
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					__result = false;
					return false;
				}

				ProjectileHitFlags hitFlags = __instance.HitFlags;
				if (thing == __instance.intendedTarget && (hitFlags & ProjectileHitFlags.IntendedTarget) != ProjectileHitFlags.None)
				{
					__result = true;
					return false;
				}
				if (thing != __instance.intendedTarget)
				{
					if (thing is Pawn pawn)
					{
						if ((hitFlags & ProjectileHitFlags.NonTargetPawns) != ProjectileHitFlags.None)
						{
							__result = true;
							return false;
						}
						if (comp.hitflags != null && comp.hitflags.hitThroughPawns && !pawn.Dead && !pawn.Downed)
						{
							thing.TakeDamage(new DamageInfo(DamageDefOf.Blunt, comp.speed * 2, 0, -1, __instance));
						}
					}
					else if ((hitFlags & ProjectileHitFlags.NonTargetWorld) != ProjectileHitFlags.None)
					{
						__result = true;
						return false;
					}
				}
				bool flewPast = false;
				if (flewPast || (comp.hitflags != null && comp.hitflags.minFillPercent > 0))
				{
					__result = thing.def.fillPercent >= comp.hitflags?.minFillPercent;
					return false;
				}
				__result = thing == __instance.intendedTarget && thing.def.fillPercent >= comp.hitflags?.minFillPercent;;
				return false;
			}
			return true;
		}

		public static void RegisterImpactCell(Thing hitThing, Projectile __instance, Thing ___launcher)
		{
			if (hitThing is VehiclePawn vehicle)
			{
				vehicle.statHandler.RegisterImpacter(___launcher, __instance.Position);
			}
		}

		public static void ExplosionDamageVehicle(Explosion explosion, Thing t, List<Thing> damagedThings, List<Thing> ignoredThings, IntVec3 cell, DamageWorker __instance, ref List<Thing> ___thingsToAffect)
		{
			if (t is VehiclePawn vehicle && !vehicle.statHandler.explosionsAffectingVehicle.Contains(explosion))
			{
				//DamageInfo dinfo = new DamageInfo(__instance.def, explosion.GetDamageAmountAt(cell), explosion.GetArmorPenetrationAt(cell), 
				//	Quaternion.LookRotation((vehicle.Position - explosion.Position).ToVector3()).eulerAngles.y, explosion.instigator, null, 
				//	explosion.weapon, DamageInfo.SourceCategory.ThingOrUnknown, explosion.intendedTarget);
				//vehicle.statHandler.TakeDamage(dinfo, cell, true);
				//vehicle.statHandler.explosionsAffectingVehicle.Add(explosion);
			}
		}

		public static void ProjectileMapToWorld(Thing __instance, DestroyMode mode = DestroyMode.Vanish)
		{
			if (__instance is Projectile projectile && projectile.GetComp<CompProjectileExitMap>() is CompProjectileExitMap exitMap)
			{
				exitMap.LeaveMap();
			}
		}
	}
}
