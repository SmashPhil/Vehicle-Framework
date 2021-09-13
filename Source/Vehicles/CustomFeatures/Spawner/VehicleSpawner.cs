using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public static class VehicleSpawner
	{
		private const int BiologicalAgeTicksMultiplier = 3600000;

		private static readonly SimpleCurve DefaultAgeGenerationCurve = new SimpleCurve
		{
			{
				new CurvePoint(0.05f, 0f),
				true
			},
			{
				new CurvePoint(0.1f, 100f),
				true
			},
			{
				new CurvePoint(0.675f, 100f),
				true
			},
			{
				new CurvePoint(0.75f, 30f),
				true
			},
			{
				new CurvePoint(0.875f, 18f),
				true
			},
			{
				new CurvePoint(1f, 10f),
				true
			},
			{
				new CurvePoint(1.125f, 3f),
				true
			},
			{
				new CurvePoint(1.25f, 0f),
				true
			}
		};

		public static VehiclePawn GenerateVehicle(VehicleDef vehicleDef, Faction faction)
		{
			return GenerateVehicle(new VehicleGenerationRequest(vehicleDef, faction));
		}

		public static VehiclePawn GenerateVehicle(VehicleGenerationRequest request)
		{
			string lastStep = "Beginning vehicle generation";
			VehiclePawn result = null;
			try
			{
				result = (VehiclePawn)ThingMaker.MakeThing(request.VehicleDef);
				lastStep = "Initializing components";
				PawnComponentsUtility.CreateInitialComponents(result);

				lastStep = "Setting faction and kindDef";
				result.kindDef = request.VehicleDef.VehicleKindDef;
				result.SetFactionDirect(request.Faction);

				lastStep = "Retrieving pattern";
				PatternDef pattern = VehicleMod.settings.vehicles.defaultGraphics.TryGetValue(result.VehicleDef.defName, result.VehicleDef.graphicData)?.pattern ?? PatternDefOf.Default;

				lastStep = "Randomized pattern check";
				result.Pattern = request.RandomizeMask ? result.VehicleGraphic.maskMatPatterns.RandomElement().Key : pattern;

				lastStep = "Initializing colors";
				result.DrawColor = request.ColorOne;
				result.DrawColorTwo = request.ColorTwo;
				result.DrawColorThree = request.ColorThree;
				result.Displacement = request.Displacement;
				result.Tiles = request.Tiling;

				lastStep = "Post Generation Setup";
				result.PostGenerationSetup();
				lastStep = "Component Post Generation Setup";
				foreach (VehicleComp comp in result.AllComps.Where(c => c is VehicleComp))
				{
					comp.PostGenerationSetup();
				}

				//REDO - Allow other modders to add setup for non clean-slate items
				if (!request.CleanSlate)
				{
					lastStep = "Randomizing upgrades";
					UpgradeAtRandom(result, request.Upgrades);
					lastStep = "Distributing ammo";
					DistributeAmmunition(result);
				}

				lastStep = "Setting age and needs";
				float num = Rand.ByCurve(DefaultAgeGenerationCurve);
				result.ageTracker.AgeBiologicalTicks = (long)(num * BiologicalAgeTicksMultiplier) + Rand.Range(0, 3600000);
				result.needs.SetInitialLevels();
				if (Find.Scenario != null)
				{
					lastStep = "Notifying Pawn Generated";
					Find.Scenario.Notify_NewPawnGenerating(result, PawnGenerationContext.NonPlayer);
				}
				lastStep = "VehiclePawn fully generated";
			}
			catch (Exception ex)
			{
				SmashLog.ErrorLabel(VehicleHarmony.LogLabel, $"Exception thrown while generating vehicle. Last Step: {lastStep}. Exception: {ex.Message}");
			}
			return result;
		}

		public static VehiclePawn SpawnVehicleRandomized(VehicleDef vehicleDef, IntVec3 cell, Map map, Faction faction, Rot4? rot = null, bool autoFill = false)
		{
			if (rot is null)
			{
				rot = Rot4.Random;
			}

			VehiclePawn vehicle = GenerateVehicle(new VehicleGenerationRequest(vehicleDef, faction, true, true));
			vehicle.CompFueledTravel?.Refuel(vehicle.CompFueledTravel.FuelCapacity);
			GenSpawn.Spawn(vehicle, cell, map, rot.Value, WipeMode.FullRefund, false);

			if (autoFill)
			{
				foreach(VehicleHandler handler in vehicle.handlers.Where(h => h.role.handlingTypes.NotNullAndAny()))
				{
					Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, faction ));
					pawn.SetFactionDirect(faction);
					vehicle.GiveLoadJob(pawn, handler);
					vehicle.Notify_Boarded(pawn);
				}
			}
			return vehicle;
		}

		public static IEnumerable<PawnKindDef> GetAppropriateVehicles(Faction faction, float points, bool combatFocused)
		{
			List<PawnKindDef> vehicles = DefDatabase<PawnKindDef>.AllDefs.Where(p => p.race.thingClass.SameOrSubclass(typeof(VehiclePawn))).ToList();
			foreach (PawnKindDef vehicleKind in vehicles)
			{
				if ( ((vehicleKind.race as VehicleDef).properties.restrictToFactions.NullOrEmpty() || (vehicleKind.race as VehicleDef).properties.restrictToFactions.Contains(faction.def)) && (vehicleKind.race as VehicleDef).vehicleTech <= faction.def.techLevel && vehicleKind.combatPower <= points)
				{
					if (combatFocused)
					{
						if ((vehicleKind.race as VehicleDef).vehicleCategory >= VehicleCategory.Combat)
						{
							yield return vehicleKind;
						}
					}
					else
					{
						yield return vehicleKind;
					}
				}
			}
		}

		private static void UpgradeAtRandom(VehiclePawn vehicle, int upgradeCount)
		{
			if (vehicle.CompUpgradeTree != null)
			{
				Rand.PushState();
				for(int i = 0; i < upgradeCount; i++)
				{
					var potentialUpgrades = vehicle.CompUpgradeTree.upgradeList.Where(u => !u.upgradeActive && vehicle.CompUpgradeTree.PrerequisitesMet(u));
					if (potentialUpgrades.NotNullAndAny())
					{
						UpgradeNode node = potentialUpgrades.RandomElement();
						vehicle.CompUpgradeTree.FinishUnlock(node);
					}
				}
				Rand.PopState();
			}
		}
		
		private static void DistributeAmmunition(VehiclePawn vehicle)
		{
			if (vehicle.CompCannons != null)
			{
				Rand.PushState();
				foreach (VehicleTurret cannon in vehicle.CompCannons.Cannons)
				{
					if (cannon.turretDef.ammunition != null)
					{
						int variation = Rand.RangeInclusive(1, cannon.turretDef.ammunition.AllowedDefCount);
						for(int i = 0; i < variation; i++)
						{
							ThingDef ammoType = cannon.turretDef.ammunition.AllowedThingDefs.ElementAt(i);
							
							int startingWeight = Rand.RangeInclusive(10, 25);
							int exponentialDecay = Rand.RangeInclusive(10, 50);
							int minReloads = Rand.RangeInclusive(2, 5);

							//{weight}e^(-{magCapacity}/{expDecay}) + {bottomLimit}
							float reloadsAvailable = (float)(startingWeight * Math.Pow(Math.E, -cannon.turretDef.magazineCapacity / exponentialDecay) + minReloads);
							Thing ammo = ThingMaker.MakeThing(ammoType);
							ammo.stackCount = Mathf.RoundToInt(cannon.turretDef.magazineCapacity * reloadsAvailable);
							vehicle.inventory.innerContainer.TryAdd(ammo, true);
						}
						cannon.AutoReloadCannon();
					}
				}
				Rand.PopState();
			}
		}
	}
}
