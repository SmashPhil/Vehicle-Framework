using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.Sound;
using RimWorld;
using UnityEngine;

namespace Vehicles
{
    public static class VehicleSpawner
    {
        public static VehiclePawn GenerateVehicle(PawnKindDef kindDef, Faction faction)
        {
            return GenerateVehicle(new VehicleGenerationRequest(kindDef, faction));
        }

        public static VehiclePawn GenerateVehicle(VehicleGenerationRequest request)
        {
            VehiclePawn result;
            try
            {
                result = GenerateVehicleInternal(request);
            }
            catch (Exception ex)
            {
                Log.Error($"Error thrown while generating VehiclePawn {request.KindDef.LabelCap} Exception: {ex.Message}");
                throw ex;
            }
            finally
            {
                //Multithreaded finalization
            }
            return result;
        }

        private static VehiclePawn GenerateVehicleInternal(VehicleGenerationRequest request)
        {
            VehiclePawn result = (VehiclePawn)ThingMaker.MakeThing(request.KindDef.race);
            PawnComponentsUtility.CreateInitialComponents(result);

            result.kindDef = request.KindDef;
            result.SetFactionDirect(request.Faction);
            result.DrawColor = request.ColorOne;
            result.DrawColorTwo = request.ColorTwo;
            result.selectedMask = request.RandomizeMask ? result.VehicleGraphic.maskMatPatterns.RandomElement().Key : "Default";
            
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
            vehicle.GetCachedComp<CompFueledTravel>()?.Refuel(vehicle.GetCachedComp<CompFueledTravel>().FuelCapacity);
            GenSpawn.Spawn(vehicle, cell, map, rot.Value, WipeMode.FullRefund, false);

            if (autoFill)
            {
                foreach(VehicleHandler handler in vehicle.GetCachedComp<CompVehicle>().handlers.Where(h => h.role.handlingTypes.AnyNullified()))
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, faction ));
                    pawn.SetFactionDirect(faction);
                    vehicle.GetCachedComp<CompVehicle>().GiveLoadJob(pawn, handler);
                    vehicle.GetCachedComp<CompVehicle>().Notify_Boarded(pawn);
                }
            }
        }

        public static IEnumerable<PawnKindDef> GetAppropriateVehicles(Faction faction, float points, bool combatFocused)
        {
            List<PawnKindDef> vehicles = DefDatabase<PawnKindDef>.AllDefs.Where(p => p.race.thingClass.SameOrSubclass(typeof(VehiclePawn))).ToList();
            foreach (PawnKindDef vehicleKind in vehicles)
            {
                CompProperties_Vehicle comp = vehicleKind.race.GetCompProperties<CompProperties_Vehicle>();
                if ( (comp.restrictToFactions.NullOrEmpty() || comp.restrictToFactions.Contains(faction.def)) && comp.vehicleTech <= faction.def.techLevel && vehicleKind.combatPower <= points)
                {
                    if (combatFocused)
                    {
                        if (comp.vehicleCategory >= VehicleCategory.Combat)
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

        private const int BiologicalAgeTicksMultiplier = 3600000;
    }
}
