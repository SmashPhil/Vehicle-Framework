using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using SmashTools;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace Vehicles
{
	internal class Compatibility_RoadsOfTheRim : ConditionalVehiclePatch
	{
		public override string PackageId => CompatibilityPackageIds.RoadsOfTheRim;

		public override void PatchAll(ModMetaData mod, Harmony harmony)
		{
			Type alertClassType = AccessTools.TypeByName("RoadsOfTheRim.HarmonyPatches.Alert_CaravanIdle_GetReport");
			harmony.Patch(original: AccessTools.Method(alertClassType, "Postfix"),
				transpiler: new HarmonyMethod(typeof(Compatibility_RoadsOfTheRim),
				nameof(GetAlertReportIdleConstructionVehicle)));

			Type gizmoClassType = AccessTools.TypeByName("RoadsOfTheRim.WorldObjectComp_Caravan");
			harmony.Patch(original: AccessTools.Method(gizmoClassType, "CaravanCurrentState"),
				postfix: new HarmonyMethod(typeof(Compatibility_RoadsOfTheRim),
				nameof(CaravanStateVehiclePather)));
		}

		private static void CaravanStateVehiclePather(WorldObjectComp __instance, ref object __result)
		{
			if (__instance.parent is VehicleCaravan vehicleCaravan && vehicleCaravan.vehiclePather.MovingNow)
			{
				__result = (byte)0; //CaravanState.Moving
			}
		}

		private static IEnumerable<CodeInstruction> GetAlertReportIdleConstructionVehicle(IEnumerable<CodeInstruction> instructions)
		{
			List<CodeInstruction> instructionList = instructions.ToList();

			FieldInfo patherField = AccessTools.Field(typeof(Caravan), nameof(Caravan.pather));
			for (int i = 0; i < instructionList.Count; i++)
			{
				CodeInstruction instruction = instructionList[i];

				if (instruction.LoadsField(patherField))
				{
					yield return new CodeInstruction(opcode: OpCodes.Call, operand: AccessTools.Method(typeof(Compatibility_RoadsOfTheRim), nameof(Compatibility_RoadsOfTheRim.CaravanMovingNow)));

					instruction = instructionList[++i]; //Ldfld : Caravan::pather
					instruction = instructionList[++i]; //CallVirt : Caravan_PathFollower::get_MovingNow()
				}
				yield return instruction;
			}
		}

		private static bool CaravanMovingNow(Caravan caravan)
		{
			if (caravan is VehicleCaravan vehicleCaravan)
			{
				return vehicleCaravan.vehiclePather.MovingNow;
			}
			return caravan.pather.MovingNow;
		}
	}
}
