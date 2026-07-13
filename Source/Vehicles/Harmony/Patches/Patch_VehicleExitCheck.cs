using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using SmashTools;
using SmashTools.Patching;
using UnityEngine;
using Verse;
using Verse.AI;
using Vehicles.World;

namespace Vehicles;

internal class Patch_VehicleExitCheck : IPatchCategory
{
	PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

	private static HashSet<Thing> _blockedVehicles;

	private static readonly AccessTools.FieldRef<Dialog_FormCaravan, Map> _getMap =
		AccessTools.FieldRefAccess<Dialog_FormCaravan, Map>("map");

	void IPatchCategory.PatchMethods()
	{
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(Dialog_FormCaravan), "PostOpen"),
			postfix: new HarmonyMethod(typeof(Patch_VehicleExitCheck),
				nameof(PostOpen_Postfix)));

		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(Dialog_FormCaravan), "PostClose"),
			postfix: new HarmonyMethod(typeof(Patch_VehicleExitCheck),
				nameof(PostClose_Postfix)));

		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(TransferableVehicleWidget), "DrawCard"),
			postfix: new HarmonyMethod(typeof(Patch_VehicleExitCheck),
				nameof(DrawCard_Postfix)));
	}

	private static bool VehicleCanExit(VehiclePawn vehicle)
	{
		Map map = vehicle.Map;
		Rot4[] dirs = [Rot4.North, Rot4.East, Rot4.South, Rot4.West];
		foreach (Rot4 dir in dirs)
		{
			if (CellFinderExtended.TryFindRandomEdgeCellWith(
				(IntVec3 cell) => !cell.Fogged(map) &&
					vehicle.CanReachVehicle(cell, PathEndMode.OnCell, Danger.Deadly) &&
					vehicle.DrivableRectOnCell(cell, Ext_Vehicles.DestinationHitboxReq.AnyRotation),
				map, dir, vehicle.VehicleDef, CellFinder.EdgeRoadChance_Always, out _))
				return true;
		}
		return false;
	}

	private static void PostOpen_Postfix(Dialog_FormCaravan __instance)
	{
		_blockedVehicles = null;

		Map map = _getMap(__instance);
		if (map == null)
			return;

		HashSet<Thing> blocked = null;

		foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
		{
			if (pawn is not VehiclePawn vehicle)
				continue;

			if (!vehicle.VehicleDef.canCaravan)
				continue;

			if (!vehicle.CanMove)
				continue;

			if (!VehicleCanExit(vehicle))
			{
				(blocked ??= new HashSet<Thing>()).Add(vehicle);
			}
		}

		_blockedVehicles = blocked;
	}

	private static void PostClose_Postfix()
	{
		_blockedVehicles = null;
	}

	private static void DrawCard_Postfix(Rect rect, TransferableOneWay transferable)
	{
		if (_blockedVehicles == null || !_blockedVehicles.Contains(transferable.AnyThing))
			return;

		Rect topHalf = rect with { height = 150f };
		const float IconSize = 24f;
		const float Gap = 4f;
		Rect iconRect = new Rect(
			topHalf.xMax - IconSize - Gap - IconSize,
			topHalf.y,
			IconSize,
			IconSize);

		GUI.DrawTexture(iconRect,
			ContentFinder<Texture2D>.Get("UI/Designators/RoadAreaOff"));
		TooltipHandler.TipRegion(iconRect,
			"VF_CaravanCantReachExit".Translate());
	}
}
