using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;
using UnityEngine;
using SmashTools;

namespace Vehicles
{
	internal class HealthAndStats : IPatchCategory
	{
		public void PatchMethods()
		{
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn), "TicksPerMove"),
				prefix: new HarmonyMethod(typeof(HealthAndStats),
				nameof(VehicleMoveSpeed)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(HealthUtility), nameof(HealthUtility.GetGeneralConditionLabel)),
				prefix: new HarmonyMethod(typeof(HealthAndStats),
				nameof(ReplaceConditionLabel)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Pawn_HealthTracker), "ShouldBeDowned"),
				prefix: new HarmonyMethod(typeof(HealthAndStats),
				nameof(VehicleShouldBeDowned)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(PawnDownedWiggler), nameof(PawnDownedWiggler.WigglerTick)),
				prefix: new HarmonyMethod(typeof(HealthAndStats),
				nameof(VehicleShouldWiggle)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(HediffUtility), nameof(HediffUtility.CanHealNaturally)),
				prefix: new HarmonyMethod(typeof(HealthAndStats),
				nameof(VehiclesDontHeal)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(HediffUtility), nameof(HediffUtility.CanHealFromTending)),
				prefix: new HarmonyMethod(typeof(HealthAndStats),
				nameof(VehiclesDontHealTended)));
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Widgets), nameof(Widgets.InfoCardButton), new Type[] { typeof(float), typeof(float), typeof(Thing) }),
				transpiler: new HarmonyMethod(typeof(HealthAndStats),
				nameof(InfoCardVehiclesTranspiler))); //REDO - change to remove info card button rather than patching the widget
			VehicleHarmony.Patch(original: AccessTools.Method(typeof(Verb_CastAbility), nameof(Verb_CastAbility.CanHitTarget)),
				prefix: new HarmonyMethod(typeof(HealthAndStats),
				nameof(VehiclesImmuneToPsycast)));
		}

		/// <summary>
		/// Apply MoveSpeed upgrade stat to vehicles
		/// </summary>
		/// <param name="diagonal"></param>
		/// <param name="__instance"></param>
		/// <param name="__result"></param>
		public static bool VehicleMoveSpeed(bool diagonal, Pawn __instance, ref int __result)
		{
			if (__instance is VehiclePawn vehicle)
			{
				float num = vehicle.GetStatValue(VehicleStatDefOf.MoveSpeed) / 60;
				float num2 = 1 / num;
				if (vehicle.Spawned && !vehicle.Map.roofGrid.Roofed(vehicle.Position))
				{
					num2 /= vehicle.Map.weatherManager.CurMoveSpeedMultiplier;
				}
				if (diagonal)
				{
					num2 *= Mathf.Sqrt(2);
				}
				__result = Mathf.RoundToInt(num2).Clamp(1, 450);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Replace vanilla labels on Boats to instead show custom ones which are modifiable in the XML defs
		/// </summary>
		/// <param name="__result"></param>
		/// <param name="pawn"></param>
		/// <param name="shortVersion"></param>
		public static bool ReplaceConditionLabel(ref string __result, Pawn pawn, bool shortVersion = false)
		{
			if (pawn != null)
			{
				if (pawn is VehiclePawn vehicle)
				{
					if (vehicle.movementStatus == VehicleMovementStatus.Offline && !pawn.Dead)
					{
						if (pawn.IsBoat() && vehicle.beached)
						{
							__result = vehicle.VehicleDef.properties.healthLabel_Beached;
						}
						else
						{
							__result = vehicle.VehicleDef.properties.healthLabel_Immobile;
						}

						return false;
					}
					if (pawn.Dead)
					{
						__result = vehicle.VehicleDef.properties.healthLabel_Dead;
						return false;
					}
					if (pawn.health.summaryHealth.SummaryHealthPercent < 0.95)
					{
						__result = vehicle.VehicleDef.properties.healthLabel_Injured;
						return false;
					}
					__result = vehicle.VehicleDef.properties.healthLabel_Healthy;
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Only allow the Boat to be downed if specified within XML def
		/// </summary>
		/// <param name="__result"></param>
		/// <param name="___pawn"></param>
		/// <returns></returns>
		public static bool VehicleShouldBeDowned(ref bool __result, ref Pawn ___pawn)
		{
			if (___pawn != null && ___pawn is VehiclePawn)
			{
				__result = false;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Only allow the Boat to wiggle if specified within the XML def
		/// </summary>
		/// <param name="___pawn"></param>
		/// <returns></returns>
		public static bool VehicleShouldWiggle(ref Pawn ___pawn)
		{
			if (___pawn != null && ___pawn is VehiclePawn vehicle)
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Vehicles do not heal over time, and must be repaired instead
		/// </summary>
		/// <param name="hd"></param>
		/// <param name="__result"></param>
		/// <returns></returns>
		public static bool VehiclesDontHeal(Hediff_Injury hd, ref bool __result)
		{
			if(hd.pawn is VehiclePawn)
			{
				__result = false;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Boats can not be tended, and thus don't heal. They must be repaired instead
		/// </summary>
		/// <param name="hd"></param>
		/// <param name="__result"></param>
		/// <returns></returns>
		public static bool VehiclesDontHealTended(Hediff_Injury hd, ref bool __result)
		{ 
			if(hd.pawn is VehiclePawn)
			{
				__result = false;
				return false;
			}
			return true;
		}

		/// <summary>
		/// Remove and replace Vehicle's info cards. Info Card is currently Work In Progress
		/// </summary>
		/// <param name="instructions"></param>
		/// <param name="ilg"></param>
		/// <returns></returns>
		public static IEnumerable<CodeInstruction> InfoCardVehiclesTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			for(int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.Calls(AccessTools.Property(typeof(Find), nameof(Find.WindowStack)).GetGetMethod()))
				{
					Label label = ilg.DefineLabel();
					///Check if pawn in question is a Boat
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_2);
					yield return new CodeInstruction(opcode: OpCodes.Isinst, operand: typeof(VehiclePawn));
					yield return new CodeInstruction(opcode: OpCodes.Brfalse, label);

					///Load a new object of type Dialog_InfoCard_Ship and load onto the WindowStack. Return after
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Property(typeof(Find), nameof(Find.WindowStack)).GetGetMethod());
					yield return new CodeInstruction(opcode: OpCodes.Ldarg_2);
					yield return new CodeInstruction(opcode: OpCodes.Newobj, operand: AccessTools.Constructor(typeof(Dialog_InfoCard_Vehicle), new Type[] { typeof(Thing) }));
					yield return new CodeInstruction(opcode: OpCodes.Callvirt, operand: AccessTools.Method(typeof(WindowStack), nameof(WindowStack.Add)));
					yield return new CodeInstruction(opcode: OpCodes.Ldc_I4_1);
					yield return new CodeInstruction(opcode: OpCodes.Ret);

					instruction.labels.Add(label);
				}

				yield return instruction;
			}
		}

		/// <summary>
		/// Block vehicles from receiving psycast effects
		/// </summary>
		/// <param name="targ"></param>
		public static bool VehiclesImmuneToPsycast(LocalTargetInfo targ)
		{
			if (targ.Pawn is VehiclePawn vehicle)
			{
				Debug.Message($"<type>Psycast</type> blocked for {vehicle}");
				return false;
			}
			return true;
		}
	}
}
