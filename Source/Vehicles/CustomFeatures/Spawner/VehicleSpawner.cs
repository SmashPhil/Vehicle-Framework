using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Vehicles
{
	public static class VehicleSpawner
	{
		private const int BiologicalAgeTicksMultiplier = 3600000;

		private static readonly SimpleCurve DefaultAgeGenerationCurve =
		[
			new CurvePoint(0.05f, 0f),
			new CurvePoint(0.1f, 100f),
			new CurvePoint(0.675f, 100f),
			new CurvePoint(0.75f, 30f),
			new CurvePoint(0.875f, 18f),
			new CurvePoint(1f, 10f),
			new CurvePoint(1.125f, 3f),
			new CurvePoint(1.25f, 0f),
		];

		public static VehiclePawn GenerateVehicle(VehicleDef vehicleDef, Faction faction)
		{
			return GenerateVehicle(new VehicleGenerationRequest(vehicleDef, faction));
		}

		public static VehiclePawn GenerateVehicle(VehicleGenerationRequest request)
		{
			if (request.VehicleDef == null)
				throw new ArgumentNullException(nameof(request.VehicleDef), "Cannot generate vehicle with null def.");

			VehiclePawn result = null;
			try
			{
				result = (VehiclePawn)ThingMaker.MakeThing(request.VehicleDef);
				result.kindDef = request.VehicleDef.kindDef;

				PawnComponentsUtility.CreateInitialComponents(result);

				result.sustainers = new VehicleSustainers(result);

				result.kindDef = request.VehicleDef.kindDef;
				result.SetFactionDirect(request.Faction);

				PatternDef pattern =
					VehicleMod.settings.vehicles.defaultGraphics
					 .TryGetValue(result.VehicleDef.defName, result.VehicleDef.graphicData)?.patternDef ??
					PatternDefOf.Default;

				result.Pattern = request.RandomizeMask ?
					DefDatabase<PatternDef>.AllDefsListForReading.RandomElementWithFallback(
						fallback: PatternDefOf.Default) :
					pattern;

				result.DrawColor = request.ColorOne;
				result.DrawColorTwo = request.ColorTwo;
				result.DrawColorThree = request.ColorThree;
				result.Displacement = request.Displacement;
				result.Tiles = request.Tiling;

				result.PostGenerationSetup();
				foreach (ThingComp comp in result.AllComps)
				{
					if (comp is VehicleComp vehicleComp)
						vehicleComp.PostGeneration();
				}

				//REDO - Allow other modders to add setup for non clean-slate items
				if (!request.CleanSlate)
				{
					UpgradeAtRandom(result, request.Upgrades);
					DistributeAmmunition(result);
				}

				float num = Rand.ByCurve(DefaultAgeGenerationCurve);
				result.ageTracker.AgeBiologicalTicks =
					(long)(num * BiologicalAgeTicksMultiplier) + Rand.Range(0, 3600000);
				result.needs.SetInitialLevels();
			}
			catch (Exception ex)
			{
				Log.Error(
					$"{VehicleHarmony.LogLabel} Exception thrown while generating {request.VehicleDef}. Exception: {ex}");
			}
			return result;
		}

		public static VehiclePawn SpawnVehicleRandomized(VehicleDef vehicleDef, IntVec3 cell, Map map,
			Faction faction, Rot4? rot = null, bool autoFill = false)
		{
			rot ??= Rot4.Random;
			VehiclePawn vehicle =
				GenerateVehicle(new VehicleGenerationRequest(vehicleDef, faction, true, true));
			vehicle.CompFueledTravel?.Refuel(vehicle.CompFueledTravel.FuelCapacity);
			GenSpawn.Spawn(vehicle, cell, map, rot.Value, WipeMode.FullRefund);

			if (autoFill)
			{
				foreach (VehicleRoleHandler handler in vehicle.handlers.Where(h =>
					h.role.HandlingTypes > HandlingType.None))
				{
					Pawn pawn =
						PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, faction));
					pawn.SetFactionDirect(faction);
					vehicle.TryAddPawn(pawn, handler);
				}
			}
			return vehicle;
		}

		private static void UpgradeAtRandom(VehiclePawn vehicle, int upgradeCount)
		{
			if (vehicle.CompUpgradeTree != null)
			{
				Rand.PushState();
				for (int i = 0; i < upgradeCount; i++)
				{
					IEnumerable<UpgradeNode> potentialUpgrades =
						vehicle.CompUpgradeTree.Props.def.nodes.Where(node =>
							!vehicle.CompUpgradeTree.NodeUnlocked(node) &&
							vehicle.CompUpgradeTree.PrerequisitesMet(node));
					if (potentialUpgrades.TryRandomElement(out UpgradeNode upgradeNode))
					{
						vehicle.CompUpgradeTree.FinishUnlock(upgradeNode);
					}
				}
				Rand.PopState();
			}
		}

		private static void DistributeAmmunition(VehiclePawn vehicle)
		{
			if (vehicle.CompVehicleTurrets != null)
			{
				Rand.PushState();
				foreach (VehicleTurret cannon in vehicle.CompVehicleTurrets.Turrets)
				{
					if (cannon.def.ammunition != null)
					{
						int variation = Rand.RangeInclusive(1, cannon.def.ammunition.AllowedDefCount);
						for (int i = 0; i < variation; i++)
						{
							ThingDef ammoType = cannon.def.ammunition.AllowedThingDefs.ElementAt(i);

							int startingWeight = Rand.RangeInclusive(10, 25);
							int exponentialDecay = Rand.RangeInclusive(10, 50);
							int minReloads = Rand.RangeInclusive(2, 5);

							// {weight}e^(-{magCapacity}/{expDecay}) + {bottomLimit}
							float reloadsAvailable =
								startingWeight * Mathf.Exp((float)-cannon.def.magazineCapacity / exponentialDecay) +
								minReloads;
							Thing ammo = ThingMaker.MakeThing(ammoType);
							ammo.stackCount = Mathf.RoundToInt(cannon.def.magazineCapacity * reloadsAvailable);
							vehicle.AddOrTransfer(ammo);
						}
						cannon.AutoReload();
					}
				}
				Rand.PopState();
			}
		}
	}
}