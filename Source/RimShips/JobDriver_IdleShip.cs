using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;
using SPExtended;

namespace Vehicles.Jobs
{
    public class JobDriver_IdleShip : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        public override string GetReport()
        {
            return Ship != null ? "AwaitOrders".Translate().ToString() : base.GetReport();
        }

        private CompVehicle Ship
        {
            get
            {
                return Pawn.TryGetComp<CompVehicle>();
            }
        }

        private Thing Pawn
        {
            get
            {
                Thing thing = job.GetTarget(TargetIndex.A).Thing;
                if(thing is null) return null;
                return thing;
            }
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil wait = new Toil
            {
                initAction = delegate ()
                {
                    base.Map.pawnDestinationReservationManager.Reserve(pawn, job, pawn.Position);
                    (pawn as VehiclePawn).vPather.StopDead();
                },
                tickAction = delegate()
                {
                    if(Ship.currentlyFishing && Ship.CanMove)
                    {
                        foreach(Pawn p in Ship.AllPawnsAboard)
                        {
                            p.skills.Learn(SkillDefOf.Animals, VehicleMod.mod.settings.FishingSkillValue, false);
                        }
                        if(Find.TickManager.TicksGame % (VehicleMod.mod.settings.fishingDelay - (Ship.AverageSkillOfCapablePawns(SkillDefOf.Animals) * (VehicleMod.mod.settings.fishingDelay/100)))  == 0)
                        {
                            KeyValuePair<ThingDef, int> fishStats;
                            bool shallowMultiplier = false;
                            if(Pawn.Map.terrainGrid.TerrainAt(Pawn.Position) == TerrainDefOf.WaterOceanDeep || Pawn.Map.terrainGrid.TerrainAt(Pawn.Position) == TerrainDefOf.WaterOceanShallow)
                            {
                                fishStats = FishingCompatibility.fishDictionarySaltWater.RandomKVPFromDictionary();
                            }
                            else
                            {
                                if(Pawn.Map.Biome == BiomeDefOf.AridShrubland && FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.Any())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Pawn.Map.Biome == BiomeDefOf.TemperateForest && FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.Any())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Pawn.Map.Biome == BiomeDefOf.Desert && FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.Any())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Pawn.Map.Biome == BiomeDefOf.TropicalRainforest && FishingCompatibility.fishDictionaryTropicalBiomeFreshWater.Any())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryTropicalBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Pawn.Map.Biome == BiomeDefOf.BorealForest && FishingCompatibility.fishDictionaryTropicalBiomeFreshWater.Any())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryTropicalBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Pawn.Map.Biome == BiomeDefOf.Tundra && FishingCompatibility.fishDictionaryColdBiomeFreshWater.Any())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryColdBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Pawn.Map.Biome == BiomeDefOf.IceSheet && FishingCompatibility.fishDictionaryColdBiomeFreshWater.Any())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryColdBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else if(Pawn.Map.Biome == BiomeDefOf.SeaIce && FishingCompatibility.fishDictionaryColdBiomeFreshWater.Any())
                                {
                                    fishStats = FishingCompatibility.fishDictionaryColdBiomeFreshWater.RandomKVPFromDictionary();
                                }
                                else
                                {
                                    fishStats = FishingCompatibility.fishDictionarySaltWater.RandomKVPFromDictionary();
                                }
                            }

                            if(Pawn.Map.terrainGrid.TerrainAt(Pawn.Position) == TerrainDefOf.WaterMovingShallow || Pawn.Map.terrainGrid.TerrainAt(Pawn.Position) == TerrainDefOf.WaterOceanShallow ||
                                Pawn.Map.terrainGrid.TerrainAt(Pawn.Position) == TerrainDefOf.WaterShallow)
                                shallowMultiplier = true;

                            float statValue = 0;
                            foreach(Pawn p in Ship.AllCapablePawns)
                            {
                                statValue += p.skills.GetSkill(SkillDefOf.Animals).Level;
                            }
                            statValue /= Ship.AllCapablePawns.Count;
                            int countByFishingSkill = (int)(fishStats.Value * (statValue/10) * (shallowMultiplier ? 0.5 : 1) * VehicleMod.mod.settings.fishingMultiplier);
                            if(countByFishingSkill <= 0) countByFishingSkill = 1;
                            Thing fish = ThingMaker.MakeThing(fishStats.Key);
                            fish.stackCount = countByFishingSkill;
                            Ship.Pawn.inventory.innerContainer.TryAdd(fish, countByFishingSkill, true);
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