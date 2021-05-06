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

		public static VehiclePawn GenerateVehicle(PawnKindDef kindDef, Faction faction)
		{
			return GenerateVehicle(new VehicleGenerationRequest(kindDef, faction));
		}

		public static VehiclePawn GenerateVehicle(VehicleGenerationRequest request)
		{
			VehiclePawn result = null;
			try
			{
				result = GenerateVehicleInternal(request);
			}
			catch (Exception ex)
			{
				Log.Error($"Error thrown while generating VehiclePawn {request.KindDef.LabelCap} Exception: {ex.Message}");
			}
			return result;
		}

		private static VehiclePawn GenerateVehicleInternal(VehicleGenerationRequest request)
		{
			VehiclePawn result = (VehiclePawn)ThingMaker.MakeThing(request.KindDef.race);
			PawnComponentsUtility.CreateInitialComponents(result);

			result.kindDef = request.KindDef;
			result.SetFactionDirect(request.Faction);
			if (result.VehicleGraphic.MatSingle.shader.SupportsMaskTex())
			{
				result.DrawColor = request.ColorOne;
				result.DrawColorTwo = request.ColorTwo;
				result.DrawColorThree = request.ColorThree;
			}
			string defaultMask = VehicleMod.settings.vehicles.defaultMasks.TryGetValue(result.VehicleDef.defName, "Default");
			PatternDef pattern = DefDatabase<PatternDef>.GetNamed(defaultMask);
			if (pattern is null)
			{
				Log.Error($"Unable to retrieve saved default pattern {defaultMask}. Defaulting to original Default mask.");
				pattern = PatternDefOf.Default;
			}
			result.pattern = request.RandomizeMask ? result.VehicleGraphic.maskMatPatterns.RandomElement().Key : pattern;
			result.PostGenerationSetup();
			foreach (VehicleComp comp in result.AllComps.Where(c => c is VehicleComp))
			{
				comp.PostGenerationSetup();
			}
			//REDO - Allow other modders to add setup for non clean-slate items
			if (!request.CleanSlate)
			{
				UpgradeAtRandom(result, request.Upgrades);
				DistributeAmmunition(result);
			}
			float num = Rand.ByCurve(DefaultAgeGenerationCurve);
			result.ageTracker.AgeBiologicalTicks = (long)(num * BiologicalAgeTicksMultiplier) + Rand.Range(0, 3600000);
			result.needs.SetInitialLevels();
			if (Find.Scenario != null)
			{
				Find.Scenario.Notify_NewPawnGenerating(result, PawnGenerationContext.NonPlayer);
			}
			return result;
		}

		public static void SpawnVehicleRandomized(PawnKindDef kindDef, IntVec3 cell, Map map, Faction faction, Rot4? rot = null, bool autoFill = false)
		{
			if (rot is null)
				rot = Rot4.Random;
			VehiclePawn vehicle = GenerateVehicle(new VehicleGenerationRequest(kindDef, faction, true, true));
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
