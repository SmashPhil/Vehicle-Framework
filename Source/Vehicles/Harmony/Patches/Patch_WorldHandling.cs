using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using SmashTools;
using SmashTools.Patching;
using UnityEngine.Assertions;
using Vehicles.World;
using Verse;
using Verse.Sound;

namespace Vehicles;

internal class Patch_WorldHandling : IPatchCategory
{
	PatchSequence IPatchCategory.PatchAt => PatchSequence.Async;

	void IPatchCategory.PatchMethods()
	{
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(WorldPawns),
        nameof(WorldPawns.GetSituation)),
			postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
				nameof(SituationBoardedVehicle)));
		HarmonyPatcher.Patch(
      original: AccessTools.Method(typeof(GameEnder),
        nameof(GameEnder.CheckOrUpdateGameOver)),
			postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
        nameof(GameEnderWithVehicles)));
		HarmonyPatcher.Patch(original: AccessTools.Method(typeof(WorldObjectsHolder), "AddToCache"),
			postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
				nameof(AddVehicleObjectToCache)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(WorldObjectsHolder), "RemoveFromCache"),
			postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
				nameof(RemoveVehicleObjectToCache)));
		HarmonyPatcher.Patch(original: AccessTools.Method(typeof(WorldObjectsHolder), "Recache"),
			prefix: new HarmonyMethod(typeof(Patch_WorldHandling),
				nameof(RecacheVehicleObjectCache)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(PawnUtility),
				nameof(PawnUtility.IsTravelingInTransportPodWorldObject)),
			postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
				nameof(AerialVehiclesDontRandomizePrisoners)));

		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(CameraJumper), nameof(CameraJumper.TryShowWorld)),
			prefix: new HarmonyMethod(typeof(Patch_WorldHandling),
				nameof(ForcedTargetingDontShowWorld)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(MainButtonWorker_ToggleWorld),
				nameof(MainButtonWorker_ToggleWorld.Activate)),
			prefix: new HarmonyMethod(typeof(Patch_WorldHandling),
				nameof(ForcedTargetingDontToggleWorld)));

		/* World Targeter Event Handling */
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(WorldTargeter), nameof(WorldTargeter.TargeterUpdate)),
			postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
				nameof(WorldTargeterUpdate)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(WorldTargeter), nameof(WorldTargeter.TargeterOnGUI)),
			postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
				nameof(WorldTargeterOnGUI)));
		HarmonyPatcher.Patch(
			original: AccessTools.Method(typeof(WorldTargeter),
				nameof(WorldTargeter.ProcessInputEvents)),
			postfix: new HarmonyMethod(typeof(Patch_WorldHandling),
				nameof(WorldTargeterProcessInputEvents)));
	}

	/// <summary>
	/// Prevent RimWorld Garbage Collection from snatching up VehiclePawn inhabitants and VehicleCaravan's VehiclePawn inhabitants by changing
	/// the WorldPawnSituation of pawns onboard vehicles
	/// </summary>
	/// <param name="p"></param>
	/// <param name="__result"></param>
	private static void SituationBoardedVehicle(Pawn p, ref WorldPawnSituation __result)
	{
		if (__result == WorldPawnSituation.Free && p.Faction != null &&
			p.Faction == Faction.OfPlayerSilentFail)
		{
			if (p is VehiclePawn)
			{
				__result = WorldPawnSituation.CaravanMember;
				return;
			}
			if (p.ParentHolder?.ParentHolder is VehiclePawn)
			{
				__result = WorldPawnSituation.CaravanMember;
			}
			if (p.GetAerialVehicle() != null)
			{
				__result = WorldPawnSituation.InTravelingTransportPod;
			}
		}
	}

	private static void GameEnderWithVehicles(GameEnder __instance, ref int ___ticksToGameOver)
	{
		if (__instance.gameEnding)
		{
			if (LandingTargeter.Instance.IsTargeting)
			{
				__instance.gameEnding = false;
				___ticksToGameOver = -1;
				return;
			}
			foreach (Map map in Find.Maps)
			{
				VehiclePositionManager positionManager =
					map.GetDetachedMapComponent<VehiclePositionManager>();
				Assert.IsNotNull(positionManager);
				foreach (VehiclePawn vehicle in positionManager.AllClaimants)
				{
					foreach (Pawn pawn in vehicle.AllPawnsAboard)
					{
						if (pawn.IsFreeColonist)
						{
							__instance.gameEnding = false;
							___ticksToGameOver = -1;
							return;
						}
					}
				}
			}
			foreach (AerialVehicleInFlight aerialVehicle in Find.World
			 .GetComponent<VehicleWorldObjectsHolder>().AerialVehicles)
			{
				foreach (Pawn pawn in aerialVehicle.Vehicle.AllPawnsAboard)
				{
					if (pawn.IsFreeColonist)
					{
						__instance.gameEnding = false;
						___ticksToGameOver = -1;
						return;
					}
				}
			}
		}
	}

	private static void AddVehicleObjectToCache(WorldObject o)
	{
		Find.World.GetComponent<VehicleWorldObjectsHolder>().AddToCache(o);
	}

	private static void RemoveVehicleObjectToCache(WorldObject o)
	{
		Find.World.GetComponent<VehicleWorldObjectsHolder>().RemoveFromCache(o);
	}

	private static void RecacheVehicleObjectCache()
	{
		Find.World.GetComponent<VehicleWorldObjectsHolder>().Recache();
	}

	private static bool ForcedTargetingDontShowWorld(ref bool __result)
	{
		if (LandingTargeter.Instance.ForcedTargeting)
		{
			__result = false;
			return false;
		}
		return true;
	}

	private static bool ForcedTargetingDontToggleWorld()
	{
		if (LandingTargeter.Instance.ForcedTargeting)
		{
			SoundDefOf.ClickReject.PlayOneShotOnCamera();
			Messages.Message("VF_MustTargetLanding".Translate(), MessageTypeDefOf.RejectInput);
			return false;
		}
		return true;
	}

	private static void AerialVehiclesDontRandomizePrisoners(Pawn pawn, ref bool __result)
	{
		if (ThingOwnerUtility.AnyParentIs<VehiclePawn>(pawn) ||
			ThingOwnerUtility.AnyParentIs<AerialVehicleInFlight>(pawn))
		{
			__result = true;
		}
	}

	/* -------------------- World Targeter -------------------- */

	private static void WorldTargeterUpdate()
	{
		Targeters.UpdateWorldTargeter();
	}

	private static void WorldTargeterOnGUI()
	{
		Targeters.OnGUIWorldTargeter();
	}

	private static void WorldTargeterProcessInputEvents()
	{
		Targeters.ProcessWorldTargeterInputEvent();
	}

	/* --------------------------------------------------------- */
}