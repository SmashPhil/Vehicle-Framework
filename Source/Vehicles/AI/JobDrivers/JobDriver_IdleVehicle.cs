using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using SmashTools;

namespace Vehicles
{
	public class JobDriver_IdleVehicle : JobDriver
	{
		protected VehiclePawn Vehicle => TargetA.Thing as VehiclePawn;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(TargetIndex.A);
			this.FailOn(() => !Vehicle.Spawned);
			yield return new Toil()
			{
				initAction = delegate()
				{
					Map.pawnDestinationReservationManager.Reserve(Vehicle, job, Vehicle.Position);
					Vehicle.vehiclePather.StopDead();
				},
				tickAction = delegate()
				{
					if (Vehicle.currentlyFishing && Vehicle.CanMoveFinal)
					{
						foreach (Pawn pawn in Vehicle.AllPawnsAboard)
						{
							pawn.skills.Learn(SkillDefOf.Animals, VehicleMod.FishingSkillValue, false);
						}
						if (Find.TickManager.TicksGame % (VehicleMod.settings.main.fishingDelay - (Vehicle.AverageSkillOfCapablePawns(SkillDefOf.Animals) * (VehicleMod.settings.main.fishingDelay/100)))  == 0)
						{
							KeyValuePair<ThingDef, int> fishStats;
							bool shallowMultiplier = false;
							if(Vehicle.Map.terrainGrid.TerrainAt(Vehicle.Position) == TerrainDefOf.WaterOceanDeep || Vehicle.Map.terrainGrid.TerrainAt(Vehicle.Position) == TerrainDefOf.WaterOceanShallow)
							{
								fishStats = FishingCompatibility.fishDictionarySaltWater.RandomKVPFromDictionary();
							}
							else
							{
								if(Vehicle.Map.Biome == BiomeDefOf.AridShrubland && FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.NotNullAndAny())
								{
									fishStats = FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.RandomKVPFromDictionary();
								}
								else if(Vehicle.Map.Biome == BiomeDefOf.TemperateForest && FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.NotNullAndAny())
								{
									fishStats = FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.RandomKVPFromDictionary();
								}
								else if(Vehicle.Map.Biome == BiomeDefOf.Desert && FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.NotNullAndAny())
								{
									fishStats = FishingCompatibility.fishDictionaryTemperateBiomeFreshWater.RandomKVPFromDictionary();
								}
								else if(Vehicle.Map.Biome == BiomeDefOf.TropicalRainforest && FishingCompatibility.fishDictionaryTropicalBiomeFreshWater.NotNullAndAny())
								{
									fishStats = FishingCompatibility.fishDictionaryTropicalBiomeFreshWater.RandomKVPFromDictionary();
								}
								else if(Vehicle.Map.Biome == BiomeDefOf.BorealForest && FishingCompatibility.fishDictionaryTropicalBiomeFreshWater.NotNullAndAny())
								{
									fishStats = FishingCompatibility.fishDictionaryTropicalBiomeFreshWater.RandomKVPFromDictionary();
								}
								else if(Vehicle.Map.Biome == BiomeDefOf.Tundra && FishingCompatibility.fishDictionaryColdBiomeFreshWater.NotNullAndAny())
								{
									fishStats = FishingCompatibility.fishDictionaryColdBiomeFreshWater.RandomKVPFromDictionary();
								}
								else if(Vehicle.Map.Biome == BiomeDefOf.IceSheet && FishingCompatibility.fishDictionaryColdBiomeFreshWater.NotNullAndAny())
								{
									fishStats = FishingCompatibility.fishDictionaryColdBiomeFreshWater.RandomKVPFromDictionary();
								}
								else if(Vehicle.Map.Biome == BiomeDefOf.SeaIce && FishingCompatibility.fishDictionaryColdBiomeFreshWater.NotNullAndAny())
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
							foreach(Pawn p in Vehicle.AllCapablePawns)
							{
								statValue += p.skills.GetSkill(SkillDefOf.Animals).Level;
							}
							statValue /= Vehicle.AllCapablePawns.Count;
							int countByFishingSkill = (int)(fishStats.Value * (statValue/10) * (shallowMultiplier ? 0.5 : 1) * VehicleMod.settings.main.fishingMultiplier);
							if(countByFishingSkill <= 0) countByFishingSkill = 1;
							Thing fish = ThingMaker.MakeThing(fishStats.Key);
							fish.stackCount = countByFishingSkill;
							Vehicle.AddOrTransfer(fish, countByFishingSkill);
						}
					}
				},
				defaultCompleteMode = ToilCompleteMode.Never
			};
		}
	}
}