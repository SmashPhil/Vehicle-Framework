using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;


namespace Vehicles.Jobs
{
    public class JobDriver_IdleShip : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        private VehiclePawn Vehicle
        {
            get
            {
                Thing thing = job.GetTarget(TargetIndex.A).Thing;
                if(thing is null) return null;
                return thing as VehiclePawn;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil wait = new Toil
            {
                initAction = delegate ()
                {
                    base.Map.pawnDestinationReservationManager.Reserve(Vehicle, job, Vehicle.Position);
                    Vehicle.vPather.StopDead();
                },
                tickAction = delegate()
                {
                    if(Vehicle.GetCachedComp<CompVehicle>().currentlyFishing && Vehicle.GetCachedComp<CompVehicle>().CanMove)
                    {
                        foreach(Pawn p in Vehicle.GetCachedComp<CompVehicle>().AllPawnsAboard)
                        {
                            p.skills.Learn(SkillDefOf.Animals, VehicleMod.settings.FishingSkillValue, false);
                        }
                        if(Find.TickManager.TicksGame % (VehicleMod.settings.fishingDelay - (Vehicle.GetCachedComp<CompVehicle>().AverageSkillOfCapablePawns(SkillDefOf.Animals) * (VehicleMod.settings.fishingDelay/100)))  == 0)
                        {
                            KeyValuePair<ThingDef, int> fishStats;
                            bool shallowMultiplier = false;
                            if(Vehicle.Map.terrainGrid.TerrainAt(Vehicle.Position) == TerrainDefOf.WaterOceanDeep || Vehicle.Map.terrainGrid.TerrainAt(Vehicle.Position) == TerrainDefOf.WaterOceanShallow)
                            {
                                fishStats = FishingCompatibility.fishDictionarySaltWater.RandomKVPFromDictionary();
                            }
                            else
                            {
                                if(Vehicle.Map.Biome == BiomeDefOf.AridShrubland && FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.AnyNullified())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Vehicle.Map.Biome == BiomeDefOf.TemperateForest && FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.AnyNullified())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Vehicle.Map.Biome == BiomeDefOf.Desert && FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.AnyNullified())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Vehicle.Map.Biome == BiomeDefOf.TropicalRainforest && FishingCompatibility.fishDictionaryTropicalBiomeFreshWater.AnyNullified())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryTropicalBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Vehicle.Map.Biome == BiomeDefOf.BorealForest && FishingCompatibility.fishDictionaryTropicalBiomeFreshWater.AnyNullified())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryTropicalBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Vehicle.Map.Biome == BiomeDefOf.Tundra && FishingCompatibility.fishDictionaryColdBiomeFreshWater.AnyNullified())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryColdBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Vehicle.Map.Biome == BiomeDefOf.IceSheet && FishingCompatibility.fishDictionaryColdBiomeFreshWater.AnyNullified())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryColdBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Vehicle.Map.Biome == BiomeDefOf.SeaIce && FishingCompatibility.fishDictionaryColdBiomeFreshWater.AnyNullified())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryColdBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else
                                {
                                    fishStats = FishingCompatibility.fishDictionarySaltWater.RandomKVPFromDictionary();
                                }
                            }

                            if(Vehicle.Map.terrainGrid.TerrainAt(Vehicle.Position) == TerrainDefOf.WaterMovingShallow || Vehicle.Map.terrainGrid.TerrainAt(Vehicle.Position) == TerrainDefOf.WaterOceanShallow ||
                                Vehicle.Map.terrainGrid.TerrainAt(Vehicle.Position) == TerrainDefOf.WaterShallow)
                                shallowMultiplier = true;

                            float statValue = 0;
                            foreach(Pawn p in Vehicle.GetCachedComp<CompVehicle>().AllCapablePawns)
                            {
                                statValue += p.skills.GetSkill(SkillDefOf.Animals).Level;
                            }
                            statValue /= Vehicle.GetCachedComp<CompVehicle>().AllCapablePawns.Count;
                            int countByFishingSkill = (int)(fishStats.Value * (statValue/10) * (shallowMultiplier ? 0.5 : 1) * VehicleMod.settings.fishingMultiplier);
                            if(countByFishingSkill <= 0) countByFishingSkill = 1;
                            Thing fish = ThingMaker.MakeThing(fishStats.Key);
                            fish.stackCount = countByFishingSkill;
                            Vehicle.inventory.innerContainer.TryAdd(fish, countByFishingSkill, true);
                        }
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Never
            };
            yield return wait;
            yield break;
        }
    }
}