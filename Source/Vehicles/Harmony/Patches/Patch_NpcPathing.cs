using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SmashTools;
using SmashTools.Patching;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;

namespace Vehicles;

internal class Patch_NpcPathing : IPatchCategory
{
	private static readonly Type JobDriverGotoDisplayClassType;

	static Patch_NpcPathing()
	{
		const string DisplayClassName = "<>c__DisplayClass1_0";

		JobDriverGotoDisplayClassType =
			typeof(JobDriver_Goto).GetNestedTypes(AccessTools.all).FirstOrDefault(type => type.Name == DisplayClassName);
		Assert.IsNotNull(JobDriverGotoDisplayClassType);
	}

	PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

	void IPatchCategory.PatchMethods()
	{
#if RAIDERS
		if (VehicleMod.settings.debug.debugAllowRaiders)
		{
			// Compiler generated methods from JobDriver_Goto::<>c__DisplayClass1_0
			List<MethodInfo> gotoMethods = JobDriverGotoDisplayClassType.GetDeclaredMethods();
			MethodInfo makeToilsDelegate0 = gotoMethods[0];
			Assert.IsTrue(makeToilsDelegate0.Name == "<MakeNewToils>b__0");
			HarmonyPatcher.Patch(original: makeToilsDelegate0,
				postfix: new HarmonyMethod(typeof(Patch_NpcPathing),
					nameof(GotoToilsFirstExit)));
			MethodInfo makeToilsDelegate6 = gotoMethods[6];
			Assert.IsTrue(makeToilsDelegate6.Name == "<MakeNewToils>b__6");
			HarmonyPatcher.Patch(original: makeToilsDelegate6,
				postfix: new HarmonyMethod(typeof(Patch_NpcPathing),
					nameof(GotoToilsSecondExit)));
		}
#endif
	}

	private static void GotoToilsFirstExit(
		JobDriver_Goto __instance /* JobDriver_goto::<>c__DisplayClass1_0 */)
	{
		TryExitMapForVehicle(__instance, false, true);
	}

	private static void GotoToilsSecondExit(
		JobDriver_Goto __instance /* JobDriver_goto::<>c__DisplayClass1_0 */)
	{
		TryExitMapForVehicle(__instance, true, true);
	}

	private static void TryExitMapForVehicle(
		JobDriver_Goto __instance /* JobDriver_goto::<>c__DisplayClass1_0 */,
		bool onEdge, bool onExitCell)
	{
		// Sticking with compiler generated notation here for ease of debugging
		JobDriver_Goto instance =
			Traverse.Create(__instance).Field("<>4__this").GetValue<JobDriver_Goto>();
		if (instance.pawn is VehiclePawn vehicle && instance.job.exitMapOnArrival && vehicle.Spawned)
		{
			Rot4 rot = CellRect.WholeMap(vehicle.Map).GetClosestEdge(vehicle.Position);
			// Only need to check 1 cell per edge, if 1 is touching then all on that edge will be.
			if (vehicle.PawnOccupiedCells(vehicle.Position, rot).Corners.Any(cell =>
				(onEdge && cell.OnEdge(vehicle.Map)) ||
				(onExitCell && vehicle.Map.exitMapGrid.IsExitCell(cell))))
			{
				PathingHelper.ExitMapForVehicle(vehicle, instance.job);
			}
		}
	}
}