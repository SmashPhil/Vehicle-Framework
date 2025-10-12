using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Patching;
using Vehicles.World;
using Verse;

namespace Vehicles;

internal class Patch_Debug : IPatchCategory
{
	PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

	void IPatchCategory.PatchMethods()
	{
		// Users do use these so they still need to be patched
		HarmonyPatcher.Patch(original: AccessTools.Method(typeof(DebugToolsSpawning), "SpawnPawn"),
			postfix: new HarmonyMethod(typeof(Patch_Debug),
				nameof(DebugHideVehiclesFromPawnSpawner)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(HealthUtility),
				nameof(HealthUtility.DamageUntilDowned)),
			prefix: new HarmonyMethod(typeof(Patch_Debug),
				nameof(DebugDamagePawnsInVehicleUntilDowned)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(HealthUtility), nameof(HealthUtility.DamageUntilDead)),
			prefix: new HarmonyMethod(typeof(Patch_Debug),
				nameof(DebugDamagePawnsInVehicleUntilDead)));

		if (DebugProperties.Debug)
		{
			HarmonyPatcher.Patch(
				original: AccessTools.Method(typeof(WorldRoutePlanner),
					nameof(WorldRoutePlanner.WorldRoutePlannerUpdate)),
				postfix: new HarmonyMethod(typeof(Patch_Debug),
					nameof(DebugSettlementPaths)));
		}

		//HarmonyPatcher.Patch(
		//	original: AccessTools.Method(typeof(DaysWorthOfFoodCalculator), "ApproxDaysWorthOfFood",
		//		parameters:
		//		[
		//			typeof(List<Pawn>), typeof(List<ThingDefCount>), typeof(PlanetTile), typeof(IgnorePawnsInventoryMode),
		//			typeof(Faction), typeof(WorldPath), typeof(float), typeof(int), typeof(bool)
		//		]),
		//	prefix: new HarmonyMethod(typeof(Patch_Debug),
		//		nameof(TestPrefix)));
		//HarmonyPatcher.Patch(
		//  original: AccessTools.Method(typeof(Dialog_Trade), "CacheTradeables"),
		//  postfix: new HarmonyMethod(typeof(Patch_Debug),
		//    nameof(TestPostfix)));
		//HarmonyPatcher.Patch(original: AccessTools.Method(typeof(Thing), "ExposeData"),
		//	finalizer: new HarmonyMethod(typeof(Debugging),
		//	nameof(ExceptionCatcher)));

		//Type modType = AccessTools.TypeByName("SaveOurShip2.TEMPStopRedErrorOnTakeoff");

		//HarmonyPatcher.Harmony.Unpatch(original: AccessTools.Method(modType, "Prefix"), HarmonyPatchType.Prefix);
		//HarmonyPatcher.Harmony.Unpatch(original: AccessTools.Method(modType, "Postfix"), HarmonyPatchType.Postfix);

		//HarmonyPatcher.Patch(original: AccessTools.Method(modType, "Postfix"),
		//	prefix: new HarmonyMethod(typeof(Debugging),
		//	nameof(TestModPatch)));
	}

	private static void TestPrefix(PlanetTile tile, WorldPath path, bool assumeCaravanMoving)
	{
		try
		{
			Log.Message($"CurrentTile: {tile} Path: {path is null} Moving: {assumeCaravanMoving}");
		}
		catch (Exception ex)
		{
			Log.Error(
				$"[Test Prefix] Exception Thrown.\nException={ex}\nInnerException={ex.InnerException}\n");
		}
	}

	private static void TestPostfix(Dialog_Trade __instance, List<Tradeable> ___cachedTradeables)
	{
		try
		{
			_ = TradeSession.deal;
		}
		catch (Exception ex)
		{
			Log.Error(
				$"[Test Postfix] Exception Thrown.\nException={ex}\nInnerException={ex.InnerException}\n");
		}
	}

	private static Exception ExceptionCatcher(Thing __instance, Exception __exception)
	{
		if (__exception != null)
		{
			Log.Message(
				$"Exception caught! Ex={__exception} Instance: {__instance}");
		}

		return __exception;
	}

	/// <summary>
	/// Removes Vehicle entries from Spawn Pawn menu, as that uses vanilla Pawn Generation whereas vehicles need special handling
	/// </summary>
	/// <param name="__result"></param>
	private static void DebugHideVehiclesFromPawnSpawner(List<DebugActionNode> __result)
	{
		for (int i = __result.Count - 1; i >= 0; i--)
		{
			string defName = __result[i].label;
			PawnKindDef pawnKindDef = DefDatabase<PawnKindDef>.GetNamed(defName);
			if (pawnKindDef?.race is VehicleDef)
			{
				__result.RemoveAt(i);
			}
		}
	}

	private static bool DebugDamagePawnsInVehicleUntilDowned(Pawn p, bool allowBleedingWounds,
		DamageDef damage,
		ThingDef sourceDef, BodyPartGroupDef bodyGroupDef)
	{
		if (p is VehiclePawn vehicle)
		{
			Pawn pawn = vehicle.AllPawnsAboard.Where(pawn => !pawn.Downed).RandomElementWithFallback();
			if (pawn is not null)
			{
				HealthUtility.DamageUntilDowned(pawn, allowBleedingWounds, damage, sourceDef,
					bodyGroupDef);
			}

			return false;
		}

		return true;
	}

	private static bool DebugDamagePawnsInVehicleUntilDead(Pawn p, DamageDef damage,
		ThingDef sourceDef, BodyPartGroupDef bodyGroupDef)
	{
		if (p is VehiclePawn vehicle)
		{
			Pawn pawn = vehicle.AllPawnsAboard.Where(pawn => !pawn.Dead).RandomElementWithFallback();
			if (pawn is not null)
			{
				HealthUtility.DamageUntilDead(pawn, damage, sourceDef, bodyGroupDef);
			}

			return false;
		}

		return true;
	}

	/// <summary>
	/// Draw paths from original settlement position to new position when moving settlement to coastline
	/// </summary>
	private static void DebugSettlementPaths()
	{
		if (!DebugProperties.DrawPaths || DebugHelper.debugLines.NullOrEmpty())
			return;

		foreach (WorldPath wp in DebugHelper.debugLines)
		{
			wp.DrawPath(null);
		}
	}

	[DebugAction(VehicleHarmony.VehiclesLabel, "Draw Hitbox Size",
		allowedGameStates = AllowedGameStates.PlayingOnMap)]
	private static void DebugDrawHitbox()
	{
		DebugTool tool = null;
		IntVec3 first;
		tool = new DebugTool("first corner...", delegate
		{
			first = UI.MouseCell();
			DebugTools.curTool = new DebugTool("second corner...", SecondCorner, first);
		});
		DebugTools.curTool = tool;
		return;

		void SecondCorner()
		{
			IntVec3 second = UI.MouseCell();
			CellRect cellRect = CellRect.FromLimits(first, second).ClipInsideMap(Find.CurrentMap);
			IntVec3 center = cellRect.ThingPositionFromRect();
			foreach (IntVec3 cell in cellRect)
			{
				IntVec3 diff = cell - center;
				Current.Game.CurrentMap.debugDrawer.FlashCell(cell, 0.75f, diff.ToIntVec2.ToString(),
					3600);
			}
			DebugTools.curTool = tool;
		}
	}

	[DebugAction(VehicleHarmony.VehiclesLabel, "Ground All Aerial Vehicles",
		allowedGameStates = AllowedGameStates.Playing)]
	private static void DebugGroundAllAerialVehicles()
	{
		foreach (AerialVehicleInFlight aerialVehicle in VehicleWorldObjectsHolder.Instance
		 .AerialVehicles)
		{
			DebugLandAerialVehicle(aerialVehicle);
		}

		foreach (Map map in Find.Maps)
		{
			foreach (Thing thing in map.spawnedThings.ToList())
			{
				if (thing is VehicleSkyfaller vehicleSkyfaller)
				{
					vehicleSkyfaller.vehicle.CompVehicleLauncher.launchProtocol.Release();
					vehicleSkyfaller.vehicle.CompVehicleLauncher.inFlight = false;
					GenSpawn.Spawn(vehicleSkyfaller.vehicle, vehicleSkyfaller.Position,
						vehicleSkyfaller.Map, vehicleSkyfaller.Rotation);
					if (VehicleMod.settings.main.deployOnLanding)
					{
						vehicleSkyfaller.vehicle.CompVehicleLauncher.SetTimedDeployment();
					}

					vehicleSkyfaller.Destroy();
				}
			}
		}
	}

	public static void DebugLandAerialVehicle(AerialVehicleInFlight aerialVehicleInFlight)
	{
		List<Settlement> playerSettlements = Find.WorldObjects.Settlements
		 .Where(s => s.Faction == Faction.OfPlayer).ToList();
		Settlement nearestSettlement = playerSettlements.MinBy(s =>
			Ext_Math.SphericalDistance(s.DrawPos, aerialVehicleInFlight.DrawPos));
		if (nearestSettlement == null)
		{
			Log.Error($"Attempting to force land aerial vehicle without a valid settlement.");
			return;
		}

		LaunchProtocol launchProtocol =
			aerialVehicleInFlight.Vehicle.CompVehicleLauncher.launchProtocol;
		Rot4 vehicleRotation = launchProtocol.LandingProperties?.forcedRotation ?? Rot4.Random;
		if (!CellFinderExtended.TryFindRandomCenterCell(nearestSettlement.Map,
			(cell) => !MapHelper.ImpassableOrVehicleBlocked(aerialVehicleInFlight.Vehicle,
				nearestSettlement.Map, cell, vehicleRotation), out IntVec3 cell))
		{
			if (!CellFinderExtended.TryRadialSearchForCell(nearestSettlement.Map.Center,
				nearestSettlement.Map, 50,
				(cell) => !MapHelper.ImpassableOrVehicleBlocked(aerialVehicleInFlight.Vehicle,
					nearestSettlement.Map, cell, vehicleRotation), out cell))
			{
				Log.Warning($"Could not find cell to spawn aerial vehicle.  Picking random cell.");
				cell = CellFinder.RandomCell(nearestSettlement.Map);
			}
		}

		VehicleSkyfaller_Arriving skyfaller =
			(VehicleSkyfaller_Arriving)VehicleSkyfallerMaker.MakeSkyfaller(aerialVehicleInFlight.Vehicle
			 .CompVehicleLauncher.Props.skyfallerIncoming, aerialVehicleInFlight.Vehicle);

		GenSpawn.Spawn(skyfaller, cell, nearestSettlement.Map, vehicleRotation);
		aerialVehicleInFlight.ClearAndDestroy();
	}
}