using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld.Planet;
using SmashTools.Patching;
using Vehicles.World;
using Verse;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace Vehicles.Compatibility;

internal class Compatibility_RailsOfTheRim : ConditionalVehiclePatch
{
	public override string PackageId => ModPackageIds.RailsOfTheRim;

	public override PatchSequence PatchAt => PatchSequence.Async;

	public override void PatchAll(ModMetaData mod)
	{
		Type alertClassType =
			AccessTools.TypeByName("RailsAndRoadsOfTheRim.Alert_CaravanIdle_GetReport");
		HarmonyPatcher.Patch(original: AccessTools.Method(alertClassType, "Postfix"),
			transpiler: new HarmonyMethod(typeof(Compatibility_RailsOfTheRim),
				nameof(GetAlertReportIdleConstructionVehicle)));

		Type gizmoClassType = AccessTools.TypeByName("RailsAndRoadsOfTheRim.WorldObjectComp_Caravan");
		HarmonyPatcher.Patch(original: AccessTools.Method(gizmoClassType, "CaravanCurrentState"),
			postfix: new HarmonyMethod(typeof(Compatibility_RailsOfTheRim),
				nameof(CaravanStateVehiclePather)));
	}

	private static void CaravanStateVehiclePather(WorldObjectComp __instance, ref object __result)
	{
		if (__instance.parent is VehicleCaravan vehicleCaravan &&
			vehicleCaravan.vehiclePather.MovingNow)
		{
			__result = (byte)0; //CaravanState.Moving
		}
	}

	private static IEnumerable<CodeInstruction> GetAlertReportIdleConstructionVehicle(
		IEnumerable<CodeInstruction> instructions)
	{
		List<CodeInstruction> instructionList = instructions.ToList();

		FieldInfo patherField = AccessTools.Field(typeof(Caravan), nameof(Caravan.pather));
		for (int i = 0; i < instructionList.Count; i++)
		{
			CodeInstruction instruction = instructionList[i];

			if (instruction.LoadsField(patherField))
			{
				yield return new CodeInstruction(opcode: OpCodes.Call,
					operand: AccessTools.Method(typeof(Compatibility_RailsOfTheRim),
						nameof(CaravanMovingNow)));

				// ReSharper disable once RedundantAssignment
				instruction = instructionList[++i]; // Ldfld : Caravan::pather
				instruction = instructionList[++i]; // CallVirt : Caravan_PathFollower::get_MovingNow()
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