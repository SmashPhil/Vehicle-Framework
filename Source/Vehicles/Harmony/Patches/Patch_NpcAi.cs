using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using SmashTools.Patching;
using UnityEngine;
using UnityEngine.Assertions;
using Vehicles.Config;
using Verse;
using Verse.AI;

namespace Vehicles;

internal class Patch_NpcAi : IPatchCategory
{
	private static readonly LinearCurve VehicleCountByPointsCurve =
	[
		new CurvePoint(0, 0),
		new CurvePoint(1000, 0),
		new CurvePoint(3000, 1),
		new CurvePoint(5000, 2),
		new CurvePoint(20000, 5),
	];

	private static readonly HashSet<PawnsArrivalModeDef> VehicleArrivalModes = [];

	PatchSequence IPatchCategory.PatchAt => PatchSequence.PostDefDatabase;

	void IPatchCategory.PatchMethods()
	{
#if RAIDERS
		if (FeatureFlags.RaidersEnabled)
		{
			VehicleArrivalModes.Add(PawnsArrivalModeDefOf.EdgeWalkIn);
			VehicleArrivalModes.Add(PawnsArrivalModeDefOf.EdgeWalkInGroups);
			VehicleArrivalModes.Add(PawnsArrivalModeDefOf.EdgeWalkInDistributed);

			//HarmonyPatcher.Patch(original: AccessTools.Method(typeof(LordJob_AssaultColony), nameof(LordJob_AssaultColony.CreateGraph)),
			//	new HarmonyMethod(typeof(NpcAi),
			//	nameof()));

			// Generation
			HarmonyPatcher.Patch(original: AccessTools.Method(typeof(PawnGroupKindWorker_Normal),
					nameof(PawnGroupKindWorker_Normal.GeneratePawns),
					parameters:
					[
						typeof(PawnGroupMakerParms), typeof(PawnGroupMaker), typeof(List<Pawn>), typeof(bool)
					]),
				prefix: new HarmonyMethod(typeof(Patch_NpcAi),
					nameof(InjectVehiclesIntoPawnKindGroupPrepare)),
				postfix: new HarmonyMethod(typeof(Patch_NpcAi),
					nameof(InjectVehiclesIntoPawnKindGroupPassthrough)));

			HarmonyPatcher.Patch(
				original: AccessTools.Method(typeof(RaidStrategyWorker),
					nameof(RaidStrategyWorker.SpawnThreats)),
				prefix: new HarmonyMethod(typeof(Patch_NpcAi),
					nameof(InjectVehiclesIntoRaidPrepare)),
				postfix: new HarmonyMethod(typeof(Patch_NpcAi),
					nameof(InjectVehiclesIntoRaidPassthrough)));

			// AI Behavior
			HarmonyPatcher.Patch(
				original: AccessTools.Method(typeof(SappersUtility),
					nameof(SappersUtility.HasBuildingDestroyerWeapon)),
				postfix: new HarmonyMethod(typeof(Patch_NpcAi),
					nameof(VehicleHasBuildingDestroyerTurret)));

#if DEBUG || UNSTABLE
			HarmonyPatcher.Patch(
				original: AccessTools.Method(typeof(JobGiver_AIFightEnemy), "TryGiveJob"),
				prefix: new HarmonyMethod(typeof(Patch_NpcAi),
					nameof(DisableVanillaJobForVehicle)));
#endif
		}
#endif
	}

	private static void InjectVehiclesIntoPawnKindGroupPrepare(PawnGroupMakerParms parms,
		PawnGroupMaker groupMaker, [UsedImplicitly] ref List<VehicleDef> __state)
	{
		Debug.Message(
			$"Attempting generation for raid. Faction={parms.faction?.def.LabelCap ?? "Null"}");
		Assert.IsNotNull(parms.faction);

		// TODO - Add vehicle injection for non-hostile raids
		if (!parms.faction.HostileTo(Faction.OfPlayer))
			return;

		VehicleRaiderDefModExtension raiderModExtension =
			parms.faction?.def.GetModExtension<VehicleRaiderDefModExtension>();
		if (raiderModExtension == null)
			return;

		Debug.Message($"[PREFIX] Generating with points: {parms.points}");
		float vehicleBudget = raiderModExtension.pointMultiplier * (parms.points - 250) / 2;
		if (vehicleBudget > 0)
		{
			float budgetSpent = 0;
			int vehicleCount = Mathf.FloorToInt(VehicleCountByPointsCurve.Evaluate(parms.points));
			if (vehicleCount <= 0)
				return;

			VehicleCategory category = RaidInjectionHelper.GetResolvedCategory(parms);
			List<VehicleDef> availableDefs = DefDatabase<VehicleDef>.AllDefsListForReading
			 .Where(vehicleDef => RaidInjectionHelper.ValidRaiderVehicle(vehicleDef, category, null,
					parms.faction, vehicleBudget)).ToList();
			Debug.Message(
				$"[PREFIX] Vehicle Budget: {vehicleBudget} AvailableDefs: {availableDefs.Count}");
			if (availableDefs.Count > 0)
			{
				__state = [];
				for (int i = 0; i < vehicleCount; i++)
				{
					VehicleDef vehicleDef = availableDefs.RandomElement();
					__state.Add(vehicleDef);
					vehicleBudget -= vehicleDef.combatPower;
					budgetSpent += vehicleDef.combatPower;
					Debug.Message($"[PREFIX] Adding {vehicleDef}");
				}
				parms.points -= budgetSpent;
			}
		}
	}

	private static void InjectVehiclesIntoPawnKindGroupPassthrough(PawnGroupMakerParms parms,
		PawnGroupMaker groupMaker, List<Pawn> outPawns, List<VehicleDef> __state)
	{
		if (!__state.NullOrEmpty())
		{
			Debug.Message($"[POSTFIX] Injecting vehicles with points: {parms.points}");
			List<Pawn> raiderHumanlikes =
				outPawns.Where(outPawns => outPawns.RaceProps.Humanlike).ToList();
			foreach (VehicleDef vehicleDef in __state)
			{
				//TODO - add check to ensure enough pawns are available to crew vehicle
				VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(
					new VehicleGenerationRequest(vehicleDef, parms.faction, randomizeColors: true,
						randomizeMask: true));
				while (vehicle.SeatsAvailable > 0 && raiderHumanlikes.Count > 0)
				{
					Pawn pawn = raiderHumanlikes.Pop();
					outPawns.Remove(pawn);
					if (!vehicle.TryAddPawn(pawn))
					{
						Log.Error($"Unable to add {pawn} to {vehicle} during raid generation.");
						outPawns.Add(pawn);
					}
				}
				outPawns.Add(vehicle);
			}
		}
	}

	private static void InjectVehiclesIntoRaidPrepare(IncidentParms parms,
		[UsedImplicitly] List<VehicleDef> __state)
	{
		if (parms.pawnKind == null || parms.faction == null)
			return;

		if (parms.faction.def == FactionDefOf.Mechanoid)
			return;

		if (parms.points > 1000 && parms.pawnCount > 5)
		{
			int vehicleCount = Mathf.FloorToInt(VehicleCountByPointsCurve.Evaluate(parms.points));
			VehicleCategory category = RaidInjectionHelper.GetResolvedCategory(parms);
			List<VehicleDef> availableDefs = DefDatabase<VehicleDef>.AllDefsListForReading
			 .Where(vehicleDef => RaidInjectionHelper.ValidRaiderVehicle(vehicleDef, category,
					parms.raidArrivalMode, parms.faction, parms.points)).ToList();
			if (vehicleCount > 0 && !availableDefs.NullOrEmpty())
			{
				__state = [];
				for (int i = 0; i < vehicleCount; i++)
				{
					VehicleDef vehicleDef = availableDefs.RandomElement();
					__state.Add(vehicleDef);
				}
			}
		}
	}

	private static void InjectVehiclesIntoRaidPassthrough(List<Pawn> __result, IncidentParms parms,
		List<VehicleDef> __state)
	{
		if (!__state.NullOrEmpty())
		{
			List<Pawn> raiderHumanlikes =
				__result.Where(outPawns => outPawns.RaceProps.Humanlike).ToList();
			foreach (VehicleDef vehicleDef in __state)
			{
				// TODO - add check to ensure enough pawns are available to crew vehicle
				VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(
					new VehicleGenerationRequest(vehicleDef, parms.faction, randomizeColors: true,
						randomizeMask: true));
				while (vehicle.SeatsAvailable > 0 && raiderHumanlikes.Count > 0)
				{
					Pawn pawn = raiderHumanlikes.Pop();
					__result.Remove(pawn);
					if (!vehicle.TryAddPawn(pawn))
					{
						Log.Error($"Unable to add {pawn} to {vehicle} during raid generation.");
						__result.Add(pawn);
					}
				}

				__result.Add(vehicle);
			}
		}
	}

#region AI Behavior

	private static void VehicleHasBuildingDestroyerTurret(ref bool __result, Pawn p)
	{
		if (!__result && p is VehiclePawn { CompVehicleTurrets: not null } vehicle)
		{
			//vehicle.CompVehicleTurrets.turrets.Any(turret => turret.ProjectileDef?.)
			__result = true; // TODO - Add configurability
		}
	}

	// NOTE - Not patched in RELEASE
	private static bool DisableVanillaJobForVehicle(Pawn pawn, ref Job __result)
	{
		if (pawn is VehiclePawn)
		{
			Trace.Fail($"{pawn.LabelCap} assigned a humanlike pawn job.");
			__result = null;
			return false;
		}

		return true;
	}

#endregion
}