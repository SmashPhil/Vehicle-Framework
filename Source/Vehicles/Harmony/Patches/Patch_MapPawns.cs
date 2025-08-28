using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using SmashTools;
using SmashTools.Patching;
using Vehicles.World;
using Verse;

namespace Vehicles;

internal class Patch_MapPawns : IPatchCategory
{
	PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

	void IPatchCategory.PatchMethods()
	{
		HarmonyPatcher.Patch(
			original: AccessTools.PropertyGetter(typeof(PawnsFinder),
				nameof(PawnsFinder.AllCaravansAndTravellingTransporters_AliveOrDead)),
			postfix: new HarmonyMethod(typeof(Patch_MapPawns),
				nameof(AllAerialVehicles_AliveOrDead)));
		HarmonyPatcher.Patch(original: AccessTools.PropertyGetter(typeof(PawnsFinder),
				nameof(PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction)),
			postfix: new HarmonyMethod(typeof(Patch_MapPawns),
				nameof(AllMapsVehiclePassengers_Alive_OfPlayerFaction)));

		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(MapPawns), "PlayerEjectablePodHolder"),
			prefix: new HarmonyMethod(typeof(Patch_MapPawns),
				nameof(PlayerEjectableVehicles)));
	}

	private static void AllAerialVehicles_AliveOrDead(ref List<Pawn> __result)
	{
		if (VehicleWorldObjectsHolder.Instance == null)
			return;
		foreach (AerialVehicleInFlight aerialVehicle in VehicleWorldObjectsHolder.Instance
		 .AerialVehicles)
		{
			__result.AddRange(aerialVehicle.Vehicle.AllPawnsAboard);
		}
	}

	private static void AllMapsVehiclePassengers_Alive_OfPlayerFaction(ref List<Pawn> __result)
	{
		if (Current.ProgramState == ProgramState.Entry)
			return;

		foreach (Map map in Find.Maps)
		{
			VehiclePositionManager positionMgr = map.GetDetachedMapComponent<VehiclePositionManager>();
			foreach (VehiclePawn vehicle in positionMgr.AllClaimants)
			{
				if (vehicle.Faction != Faction.OfPlayer)
					continue;
				if (vehicle.AllPawnsAboard.Count == 0)
					continue;

				foreach (Pawn pawn in vehicle.AllPawnsAboard)
				{
					if (pawn.Faction == Faction.OfPlayer)
						__result.Add(pawn);
				}
			}
		}
	}

	private static bool PlayerEjectableVehicles(Thing thing, ref IThingHolder __result)
	{
		if (thing is VehiclePawn vehicle)
		{
			__result = vehicle;
			return false;
		}
		return true;
	}
}