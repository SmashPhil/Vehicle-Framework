using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Patching;
using Vehicles.World;
using Verse;

namespace Vehicles;

internal class Patch_MapHandling : IPatchCategory
{
	private static readonly FastInvokeHandler IsValidColonyPawn;

	static Patch_MapHandling()
	{
		IsValidColonyPawn = MethodInvoker.GetHandler(AccessTools.Method(typeof(MapPawns), "IsValidColonyPawn"));
	}

	PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

	void IPatchCategory.PatchMethods()
	{
		HarmonyPatcher.Patch(
			original: AccessTools.PropertyGetter(typeof(TileMutatorWorker_Coast), "CoastOffset"),
			postfix: new HarmonyMethod(typeof(Patch_MapHandling),
				nameof(CoastSizeMultiplier)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(TileMutatorWorker_River), "GetRiverWidthAt"),
			postfix: new HarmonyMethod(typeof(Patch_MapHandling),
				nameof(RiverNodeWidth)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(TileFinder),
				nameof(TileFinder.RandomSettlementTileFor),
				parameters: [typeof(PlanetLayer), typeof(Faction), typeof(bool), typeof(Predicate<PlanetTile>)]),
			postfix: new HarmonyMethod(typeof(Patch_MapHandling),
				nameof(AdjustSettlement)));
		HarmonyPatcher.Patch(
			original: AccessTools.Property(typeof(MapPawns), nameof(MapPawns.AnyPawnBlockingMapRemoval))
			 .GetGetMethod(),
			postfix: new HarmonyMethod(typeof(Patch_MapHandling),
				nameof(AnyVehicleBlockingMapRemoval)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(GasGrid), nameof(GasGrid.GasCanMoveTo)),
			postfix: new HarmonyMethod(typeof(Patch_MapHandling),
				nameof(GasCanMoveThroughVehicle)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(MapInterface), nameof(MapInterface.MapInterfaceUpdate)),
			postfix: new HarmonyMethod(typeof(Patch_MapHandling),
				nameof(DebugUpdateVehicleRegions)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(MapInterface),
				nameof(MapInterface.MapInterfaceOnGUI_AfterMainTabs)),
			postfix: new HarmonyMethod(typeof(Patch_MapHandling),
				nameof(DebugOnGUIVehicleRegions)));
	}

	/// <summary>
	/// Modify the randomized coastline width to enable coastal travel to operate more smoothly.
	/// </summary>
	private static void CoastSizeMultiplier(ref FloatRange __result)
	{
		__result = ModSettingsHelper.BeachMultiplier(__result);
	}

	/// <summary>
	/// Apply ModSettings multiplier to river size to enable players to tweak the map to
	/// better suit vehicles. (eg. more water for boats or less for more land vehicle usage)
	/// </summary>
	private static void RiverNodeWidth(ref float __result)
	{
		__result *= ModSettingsHelper.RiverMultiplier;
	}

	/// <summary>
	/// Move settlement's spawning location towards the coastline with radius r specified in the mod settings
	/// </summary>
	public static void AdjustSettlement(ref PlanetTile __result)
	{
		if (TestWatcher.RunningTests)
			return;
		if (VehicleMod.settings.main.adjustSettlementRadius == 0)
			return;

		__result = WorldHelper.AdjustSettlement(__result);
	}

	/// <summary>
	/// Ensure map is not removed with vehicles that contain pawns or maps currenty being targeted for landing.
	/// </summary>
	public static void AnyVehicleBlockingMapRemoval(ref bool __result, Map ___map)
	{
		if (__result is false)
		{
			if (LandingTargeter.Instance.IsTargeting && Current.Game.CurrentMap == ___map ||
				MapHelper.AnyAerialVehiclesInRecon(___map))
			{
				__result = true;
				return;
			}
			foreach (VehiclePawn vehicle in ___map.GetDetachedMapComponent<VehiclePositionManager>()
			 .AllClaimants)
			{
				if (vehicle.MovementPermissions.HasFlag(VehiclePermissions.Autonomous))
				{
					__result = true;
					return;
				}

				foreach (Pawn passenger in vehicle.AllPawnsAboard)
				{
					if ((bool)IsValidColonyPawn.Invoke(___map.mapPawns, passenger))
					{
						__result = true;
						return;
					}
				}
			}
		}
	}

	private static void GasCanMoveThroughVehicle(IntVec3 cell, ref bool __result, Map ___map)
	{
		if (__result)
		{
			VehiclePawn vehicle =
				___map.GetDetachedMapComponent<VehiclePositionManager>().ClaimedBy(cell);
			__result = vehicle == null || vehicle.VehicleDef.Fillage != FillCategory.Full;
		}
	}

	public static void DebugUpdateVehicleRegions()
	{
		if (Find.CurrentMap != null && !WorldRendererUtility.WorldRendered &&
			DebugHelper.AnyDebugSettings)
		{
			DebugHelper.DebugDrawVehicleRegion(Find.CurrentMap);
		}
	}

	public static void DebugOnGUIVehicleRegions()
	{
		if (Find.CurrentMap != null && !WorldRendererUtility.WorldRendered &&
			DebugHelper.AnyDebugSettings)
		{
			DebugHelper.DebugDrawVehiclePathCostsOverlay(Find.CurrentMap);
		}
	}
}