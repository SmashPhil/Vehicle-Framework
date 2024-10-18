#define ENABLE_RAIDERS

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;
using RimWorld.Planet;
using HarmonyLib;
using SmashTools;
using static SmashTools.Debug;

namespace Vehicles
{
	internal class NPCAI : IPatchCategory
	{
		private static readonly LinearCurve raidersToReplaceCurve = new LinearCurve()
		{
			new CurvePoint(1, 0),
			new CurvePoint(5, 0),
			new CurvePoint(8, 1),
			new CurvePoint(14, 2),
			new CurvePoint(20, 3),
			new CurvePoint(40, 5),
			new CurvePoint(100, 10),
			new CurvePoint(150, 20),
		};

		private static readonly HashSet<PawnsArrivalModeDef> vehicleArrivalModes = new HashSet<PawnsArrivalModeDef>();

		private static readonly List<VehicleDef> availableVehicleDefs = new List<VehicleDef>();

		public void PatchMethods()
		{
#if (UNSTABLE || DEBUG) && ENABLE_RAIDERS
			if (VehicleMod.settings.debug.debugAllowRaiders)
			{
				vehicleArrivalModes.Add(PawnsArrivalModeDefOf.EdgeWalkIn);
				vehicleArrivalModes.Add(PawnsArrivalModeDefOf.EdgeWalkInGroups);
				vehicleArrivalModes.Add(PawnsArrivalModeDefOf.EdgeWalkInDistributed);

				//VehicleHarmony.Patch(original: AccessTools.Method(typeof(LordJob_AssaultColony), nameof(LordJob_AssaultColony.CreateGraph)),
				//	new HarmonyMethod(typeof(NPCAI),
				//	nameof()));

				// Generation
				VehicleHarmony.Patch(original: AccessTools.Method(typeof(PawnGroupKindWorker_Normal), nameof(PawnGroupKindWorker_Normal.GeneratePawns),
						parameters: [typeof(PawnGroupMakerParms), typeof(PawnGroupMaker), typeof(List<Pawn>), typeof(bool)]),
					prefix: new HarmonyMethod(typeof(NPCAI),
					nameof(InjectVehiclesIntoPawnKindGroupPrepare)),
					postfix: new HarmonyMethod(typeof(NPCAI),
					nameof(InjectVehiclesIntoPawnKindGroupPassthrough)));

				VehicleHarmony.Patch(original: AccessTools.Method(typeof(RaidStrategyWorker), nameof(RaidStrategyWorker.SpawnThreats)),
					prefix: new HarmonyMethod(typeof(NPCAI),
					nameof(InjectVehiclesIntoRaidPrepare)),
					postfix: new HarmonyMethod(typeof(NPCAI),
					nameof(InjectVehiclesIntoRaidPassthrough)));

				// AI Behavior
#if !RELEASE

				VehicleHarmony.Patch(original: AccessTools.Method(typeof(JobGiver_AIFightEnemy), "TryGiveJob"),
					prefix: new HarmonyMethod(typeof(NPCAI),
					nameof(DisableVanillaJobForVehicle)));
#endif
			}
#endif
		}

		private static void InjectVehiclesIntoPawnKindGroupPrepare(PawnGroupMakerParms parms, PawnGroupMaker groupMaker)
		{
			Debug.Message($"Attempting generation for raid. Faction={parms.faction?.def.LabelCap ?? "Null"}");
			if (parms.faction == null)
			{
				return;
			}
			var raiderModExtension = parms.faction.def.GetModExtension<VehicleRaiderDefModExtension>();
			if (raiderModExtension == null)
			{
				return;
			}
			HashSet<PawnsArrivalModeDef> allowedArrivalModes = raiderModExtension.arrivalModes ?? vehicleArrivalModes;
			
			if (!availableVehicleDefs.NullOrEmpty())
			{
				Log.Warning($"Injecting vehicles into PawnKingGroup when previous iteration hasn't finished.");
			}
			Debug.Message($"[PREFIX] Generating with points: {parms.points}");
			float vehicleBudget = raiderModExtension.pointMultiplier * (parms.points - 250) / 2;
			if (vehicleBudget > 0)
			{
				float budgetSpent = 0;
				int vehicleCount = 1;// Mathf.FloorToInt(raidersToReplaceCurve.Evaluate(parms.pawnCount));
				VehicleCategory category = RaidInjectionHelper.GetResolvedCategory(parms);
				List<VehicleDef> availableDefs = DefDatabase<VehicleDef>.AllDefsListForReading
					.Where(vehicleDef => RaidInjectionHelper.ValidRaiderVehicle(vehicleDef, category, null, parms.faction, vehicleBudget)).ToList();
				Debug.Message($"[PREFIX] Vehicle Budget: {vehicleBudget} AvailableDefs: {availableDefs.Count}");
				if (vehicleCount > 0 && !availableDefs.NullOrEmpty()) 
				{
					availableVehicleDefs.Clear();
					for (int i = 0; i < vehicleCount; i++)
					{
						VehicleDef vehicleDef = availableDefs.RandomElement();
						availableVehicleDefs.Add(vehicleDef);
						vehicleBudget -= vehicleDef.combatPower;
						budgetSpent += vehicleDef.combatPower;
						Debug.Message($"[PREFIX] Adding {vehicleDef}");
					}
					parms.points -= budgetSpent;
				}
			}
		}

		private static void InjectVehiclesIntoPawnKindGroupPassthrough(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns)
		{
			if (!availableVehicleDefs.NullOrEmpty())
			{
				Debug.Message($"[POSTFIX] Injecting vehicles with points: {parms.points}");
				List<Pawn> raiderHumanlikes = outPawns.Where(outPawns => outPawns.RaceProps.Humanlike).ToList();
				foreach (VehicleDef vehicleDef in availableVehicleDefs)
				{
					//TODO - add check to ensure enough pawns are available to crew vehicle
					VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(new VehicleGenerationRequest(vehicleDef, parms.faction, randomizeColors: true, randomizeMask: true));
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
				availableVehicleDefs.Clear();
			}
		}

		private static void InjectVehiclesIntoRaidPrepare(IncidentParms parms, List<VehicleDef> __state)
		{
			if (parms.pawnKind != null)
			{
				if (parms.faction == null || parms.faction.def == FactionDefOf.Mechanoid)
				{
					return;
				}

				if (parms.points > 1000 && parms.pawnCount > 5)
				{
					int vehicleCount = Mathf.FloorToInt(raidersToReplaceCurve.Evaluate(parms.pawnCount));
					VehicleCategory category = RaidInjectionHelper.GetResolvedCategory(parms);
					List<VehicleDef> availableDefs = DefDatabase<VehicleDef>.AllDefsListForReading.Where(vehicleDef => RaidInjectionHelper.ValidRaiderVehicle(vehicleDef, category, parms.raidArrivalMode, parms.faction, parms.points)).ToList();
					if (vehicleCount > 0 && !availableDefs.NullOrEmpty())
					{
						__state = new List<VehicleDef>();
						for (int i = 0; i < vehicleCount; i++)
						{
							VehicleDef vehicleDef = availableDefs.RandomElement();
							__state.Add(vehicleDef);
						}
					}
				}
			}
		}

		private static void InjectVehiclesIntoRaidPassthrough(List<Pawn> __result, IncidentParms parms, List<VehicleDef> __state)
		{
			if (!__state.NullOrEmpty())
			{
				List<Pawn> raiderHumanlikes = __result.Where(outPawns => outPawns.RaceProps.Humanlike).ToList();
				foreach (VehicleDef vehicleDef in __state)
				{
					//TODO - add check to ensure enough pawns are available to crew vehicle
					VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(new VehicleGenerationRequest(vehicleDef, parms.faction, randomizeColors: true, randomizeMask: true));
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
		

		private static bool DisableVanillaJobForVehicle(Pawn pawn, ref Job __result)
		{
			if (pawn is VehiclePawn)
			{
				Assert(false, $"Vehicle should never even try to be assigned this job. Vehicle={pawn.LabelCap}");
				__result = null;
				return false;
			}
			return true;
		}

		#endregion
	}
}
