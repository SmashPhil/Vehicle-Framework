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

		public void PatchMethods()
		{
			if (VehicleMod.settings.debug.debugAllowRaiders)
			{
				//TODO - Look into IncidentParmsUtility instead
				VehicleHarmony.Patch(original: AccessTools.Method(typeof(RaidStrategyWorker), nameof(RaidStrategyWorker.SpawnThreats)),
				prefix: new HarmonyMethod(typeof(NPCAI),
				nameof(InjectVehiclesIntoRaidPrepare)),
				postfix: new HarmonyMethod(typeof(NPCAI),
				nameof(InjectVehiclesIntoRaidPassthrough)));
			}
			
		}

		private static void InjectVehiclesIntoRaidPrepare(IncidentParms parms, List<VehicleDef> __state)
		{
			Debug.Message($"Attempting generation for raid. Invalid: {parms.pawnKind is null} Faction={parms.faction?.def.LabelCap ?? "Null"}");
			if (parms.pawnKind != null)
			{
				if (parms.faction == null || parms.faction.def == FactionDefOf.Mechanoid)
				{
					return;
				}

				Debug.Message($"[PREFIX] Generating with points: {parms.points}");
				if (parms.points > 1000 && parms.pawnCount > 5)
				{
					int vehicleCount = Mathf.FloorToInt(raidersToReplaceCurve.Evaluate(parms.pawnCount));
					List<VehicleDef> availableDefs = DefDatabase<VehicleDef>.AllDefsListForReading.Where(vehicleDef => ValidRaiderVehicle(vehicleDef, parms)).ToList();
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
				Debug.Message($"[POSTFIX] Injecting vehicles with points: {parms.points}");
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

		private static bool ValidRaiderVehicle(VehicleDef vehicleDef, IncidentParms parms)
		{
			if (vehicleDef.vehicleType != VehicleType.Land)
			{
				return false;
			}
			if (vehicleDef.combatPower > parms.points)
			{
				return false;
			}
			if (parms.faction.def.techLevel < vehicleDef.techLevel)
			{
				return false;
			}
			return vehicleDef.enabled.HasFlag(VehicleEnabledFor.Raiders);
		}
	}
}
