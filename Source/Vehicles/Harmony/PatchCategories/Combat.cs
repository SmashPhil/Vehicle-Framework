using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;
using SmashTools;

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
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Projectile), "ImpactSomething"),
				transpiler: new HarmonyMethod(typeof(Combat),
				nameof(VehicleProjectileChanceToHit)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Thing), nameof(Thing.Destroy)),
				prefix: new HarmonyMethod(typeof(Combat),
				nameof(ProjectileMapToWorld)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Projectile), "CheckForFreeIntercept"),
				transpiler: new HarmonyMethod(typeof(Combat),
				nameof(VehicleProjectileInterceptor)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Explosion), "AffectCell"),
				prefix: new HarmonyMethod(typeof(Combat),
				nameof(AffectVehicleInCell)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(DamageWorker), "ExplosionDamageThing"),
				postfix: new HarmonyMethod(typeof(Combat),
				nameof(VehicleMultipleExplosionInstances)),
				transpiler: new HarmonyMethod(typeof(Combat),
				nameof(VehicleExplosionDamageTranspiler)));
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

		public static IEnumerable<CodeInstruction> VehicleProjectileChanceToHit(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder localBuilder && localBuilder.LocalIndex == 8)
				{
					yield return instruction; //Stloc_S : 8
					instruction = instructionList[++i];
					yield return instruction; //Ldloc_S : 8
					instruction = instructionList[++i];
					yield return new CodeInstruction(opcode: OpCodes.Call, AccessTools.Method(typeof(Combat), nameof(VehiclePawnFillageInterceptReroute)));
				}

				yield return instruction;
			}
		}

		public static void ProjectileMapToWorld(Thing __instance, DestroyMode mode = DestroyMode.Vanish)
		{
			if (__instance is Projectile projectile && projectile.GetComp<CompProjectileExitMap>() is CompProjectileExitMap exitMap)
			{
				exitMap.LeaveMap();
			}
		}

		public static IEnumerable<CodeInstruction> VehicleProjectileInterceptor(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.opcode == OpCodes.Stloc_S && instruction.operand is LocalBuilder localBuilder && localBuilder.LocalIndex == 7)
				{
					yield return instruction; //Stloc_S : 7
					instruction = instructionList[++i];
					yield return instruction; //Ldloc_S : 7
					instruction = instructionList[++i];
					yield return new CodeInstruction(opcode: OpCodes.Call, AccessTools.Method(typeof(Combat), nameof(VehiclePawnFillageInterceptReroute)));
				}

				yield return instruction;
			}
		}

		public static bool AffectVehicleInCell(Explosion __instance, IntVec3 c)
		{
			if (__instance.Map.GetCachedMapComponent<VehiclePositionManager>().ClaimedBy(c) is VehiclePawn vehicle)
			{
				//If cell is not on edge of vehicle, block explosion
				return vehicle.OccupiedRect().EdgeCells.Contains(c);
			}
			return true;
		}

		public static void VehicleMultipleExplosionInstances(Thing t, ref List<Thing> damagedThings, List<Thing> ignoredThings, IntVec3 cell)
		{
			if (t is VehiclePawn vehicle)
			{
				if (ignoredThings != null && ignoredThings.Contains(t))
				{
					return;
				}
				damagedThings.Remove(vehicle);
			}
		}

		public static IEnumerable<CodeInstruction> VehicleExplosionDamageTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();
			MethodInfo takeDamageMethod = AccessTools.Method(typeof(Thing), nameof(Thing.TakeDamage));
			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(takeDamageMethod))
				{
					//Clear stack for rerouted call
					yield return new CodeInstruction(opcode: OpCodes.Pop); //ldarg.2 : thing
					yield return new CodeInstruction(opcode: OpCodes.Pop); //ldloc.1 : dinfo

					yield return new CodeInstruction(opcode: OpCodes.Ldloc_1); //dinfo
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_2); //thing
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_S, 5); //cell
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Combat), nameof(Combat.TakeDamageReroute)));

					instruction = instructionList[++i];
				}
				yield return instruction;
			}
		}

		private static DamageWorker.DamageResult TakeDamageReroute(DamageInfo dinfo, Thing thing, IntVec3 cell)
		{
			if (thing is VehiclePawn vehicle && vehicle.TryTakeDamage(dinfo, cell, out var result))
			{
				return result;
			}
			return thing.TakeDamage(dinfo);
		}

		private static Pawn VehiclePawnFillageInterceptReroute(Pawn pawn)
		{
			if (pawn is VehiclePawn)
			{
				//if pawn is vehicle, assign back to null to avoid "stance" based interception chance, and fall through to fillage.
				return null;
			}
			return pawn;
		}
	}
}
