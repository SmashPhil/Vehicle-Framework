using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using SmashTools;
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
			if (request.vehicleDef == null)
				throw new ArgumentNullException(nameof(request.vehicleDef), "Cannot generate vehicle with null def.");

			VehiclePawn result = null;
			try
			{
				result = (VehiclePawn)ThingMaker.MakeThing(request.vehicleDef);
				result.kindDef = request.vehicleDef.kindDef;

				PawnComponentsUtility.CreateInitialComponents(result);

				result.sustainers = new VehicleSustainers(result);

				result.kindDef = request.vehicleDef.kindDef;
				result.SetFactionDirect(request.faction);

				PatternData patternData = GetColors(request.vehicleDef, randomize: request.randomizeColors);

				result.DrawColor = patternData.color;
				result.DrawColorTwo = patternData.colorTwo;
				result.DrawColorThree = patternData.colorThree;
				result.Displacement = patternData.displacement;
				result.Tiles = patternData.tiles;

				result.PostGenerationSetup();
				foreach (ThingComp comp in result.AllComps)
				{
					if (comp is VehicleComp vehicleComp)
						vehicleComp.PostGeneration();
				}

				//REDO - Allow other modders to add setup for non clean-slate items
				if (!request.cleanSlate)
				{
					//UpgradeAtRandom(result, request.Upgrades);
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
					$"{VehicleHarmony.LogLabel} Exception thrown while generating {request.vehicleDef}. Exception: {ex}");
			}
			return result;

			static PatternData GetColors(VehicleDef vehicleDef, bool randomize)
			{
				using RandStateBlock rsb = new();

				PatternData patternData = new();

				if (!randomize)
				{
					PatternData fallback = vehicleDef.graphicData;
					fallback ??= new PatternData(Color.white, Color.white, Color.white,
						PatternDefOf.Default, displacement: Vector2.zero, tiles: 0);
					GraphicDataRGB graphicData =
						VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(vehicleDef.defName, fallback: fallback);
					patternData.Copy(graphicData);
					return patternData;
				}

				patternData.patternDef = DefDatabase<PatternDef>.AllDefsListForReading.RandomElementWithFallback(
					fallback: PatternDefOf.Default);
				if (Rand.Chance(0.1f))
				{
					(patternData.color, patternData.colorTwo, patternData.colorThree) =
						VehicleGenerationRequest.GetCompletelyRandomColors();
				}
				else
				{
					(patternData.color, patternData.colorTwo, patternData.colorThree) =
						VehicleMod.settings.colorStorage.GetRandomPalette();
				}
				Vector2 displacement = new(Rand.Range(-1.5f, 1.5f), Rand.Range(-1.5f, 1.5f));
				patternData.displacement = displacement;
				float tiling = Rand.Range(0.25f, 1.5f);
				patternData.tiles = tiling;
				return patternData;
			}
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
			if (vehicle.CompVehicleTurrets == null)
				return;

			using RandStateBlock rsb = new();
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
		}
	}
}