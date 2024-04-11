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

		private static readonly List<VehicleDef> availableVehicleDefs = new List<VehicleDef>();

		public void PatchMethods()
		{
			if (VehicleMod.settings.debug.debugAllowRaiders)
			{
				VehicleHarmony.Patch(original: AccessTools.Method(typeof(PawnGroupKindWorker_Normal), nameof(PawnGroupKindWorker_Normal.GeneratePawns), 
						parameters: new Type[] { typeof(PawnGroupMakerParms), typeof(PawnGroupMaker), typeof(List<Pawn>), typeof(bool) }),
					prefix: new HarmonyMethod(typeof(NPCAI),
					nameof(InjectVehiclesIntoPawnKindGroupPrepare)),
					postfix: new HarmonyMethod(typeof(NPCAI),
					nameof(InjectVehiclesIntoPawnKindGroupPassthrough)));

				VehicleHarmony.Patch(original: AccessTools.Method(typeof(RaidStrategyWorker), nameof(RaidStrategyWorker.SpawnThreats)),
					prefix: new HarmonyMethod(typeof(NPCAI),
					nameof(InjectVehiclesIntoRaidPrepare)),
					postfix: new HarmonyMethod(typeof(NPCAI),
					nameof(InjectVehiclesIntoRaidPassthrough)));
			}
		}

		private static void InjectVehiclesIntoPawnKindGroupPrepare(PawnGroupMakerParms parms, PawnGroupMaker groupMaker)
		{
			Debug.Message($"Attempting generation for raid. Faction={parms.faction?.def.LabelCap ?? "Null"}");
			if (parms.faction == null || parms.faction.def == FactionDefOf.Mechanoid)
			{
				return;
			}

			if (!availableVehicleDefs.NullOrEmpty())
			{
				Log.Warning($"Injecting vehicles into PawnKingGroup when previous iteration hasn't finished.");
			}

			Debug.Message($"[PREFIX] Generating with points: {parms.points}");
			float vehicleBudget = (parms.points - 250) / 2;
			if (vehicleBudget > 0)
			{
				float budgetSpent = 0;
				int vehicleCount = 1;// Mathf.FloorToInt(raidersToReplaceCurve.Evaluate(parms.pawnCount));
				VehicleCategory category = RaidInjectionHelper.GetResolvedCategory(parms);
				List<VehicleDef> availableDefs = DefDatabase<VehicleDef>.AllDefsListForReading.Where(vehicleDef => RaidInjectionHelper.ValidRaiderVehicle(vehicleDef, category, null, parms.faction, vehicleBudget)).ToList();
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
				}
				parms.points -= budgetSpent;
			}
		}

		private static void InjectVehiclesIntoPawnKindGroupPassthrough(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, List<Pawn> outPawns)
		{
			if (!availableVehicleDefs.NullOrEmpty())
			{
				Debug.Message($"[POSTFIX] Injecting vehicles with points: {parms.points}");
				List<VehiclePawn> vehicles = new List<VehiclePawn>();
				foreach (VehicleDef vehicleDef in availableVehicleDefs)
				{
					//TODO - add check to ensure enough pawns are available to crew vehicle
					VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(new VehicleGenerationRequest(vehicleDef, parms.faction, randomizeColors: true, randomizeMask: true));
					int crewCount = vehicle.PawnCountToOperateLeft;
					for (int i = crewCount; i >= 0; i--)
					{
						Pawn pawn = outPawns.Pop();
						vehicle.Notify_Boarded(pawn);
					}
					vehicles.Add(vehicle);
				}
				outPawns.AddRange(vehicles);

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

		private static void InjectVehiclesIntoRaidPassthrough(ref List<Pawn> __result, IncidentParms parms, List<VehicleDef> __state)
		{
			if (!__state.NullOrEmpty())
			{
				List<VehiclePawn> vehicles = new List<VehiclePawn>();
				foreach (VehicleDef vehicleDef in __state)
				{
					//TODO - add check to ensure enough pawns are available to crew vehicle
					VehiclePawn vehicle = VehicleSpawner.GenerateVehicle(new VehicleGenerationRequest(vehicleDef, parms.faction, randomizeColors: true, randomizeMask: true));
					int crewCount = vehicle.PawnCountToOperateLeft;
					for (int i = crewCount; i >= 0; i--)
					{
						Pawn pawn = __result.Pop();
						vehicle.Notify_Boarded(pawn);
					}
					vehicles.Add(vehicle);
				}
				__result.AddRange(vehicles);
			}
		}
	}
}
