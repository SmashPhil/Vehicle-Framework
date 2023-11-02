using System.Collections.Generic;
using Verse;
using Verse.AI;
using RimWorld;
using SmashTools;
using UnityEngine;

namespace Vehicles
{
	public class JobDriver_IdleVehicle : JobDriver
	{
		protected VehiclePawn Vehicle => TargetA.Thing as VehiclePawn;

		protected int TicksToFish => VehicleMod.settings.main.fishingDelay - Mathf.RoundToInt(Vehicle.AverageSkillOfCapablePawns(SkillDefOf.Animals) / 20f * VehicleMod.settings.main.fishingDelay) + VehicleMod.settings.main.fishingDelay;

		public override bool TryMakePreToilReservations(bool errorOnFailed)
		{
			return true;
		}

		protected override IEnumerable<Toil> MakeNewToils()
		{
			this.FailOnDestroyedOrNull(TargetIndex.A);
			this.FailOn(() => !Vehicle.Spawned);
			int ticksTillFish = int.MaxValue;
			yield return new Toil()
			{
				initAction = delegate()
				{
					ticksTillFish = TicksToFish;
					Map.pawnDestinationReservationManager.Reserve(Vehicle, job, Vehicle.Position);
					Vehicle.vehiclePather.StopDead();
				},
				tickAction = delegate()
				{
					if (Vehicle.currentlyFishing && Vehicle.CanMoveFinal)
					{
						ticksTillFish--;
						foreach (Pawn pawn in Vehicle.AllPawnsAboard)
						{
							pawn.skills.Learn(SkillDefOf.Animals, VehicleMod.FishingSkillValue, false);
						}
						if (ticksTillFish <= 0)
						{
							ticksTillFish = TicksToFish;
							ThingDef fishDef = FishingCompatibility.FetchViableFish(Vehicle.Map.Biome, Vehicle.Map.terrainGrid.TerrainAt(Vehicle.Position));
							
							float statValue = 0;
							foreach (Pawn pawn in Vehicle.AllCapablePawns)
							{
								statValue += pawn.skills.GetSkill(SkillDefOf.Animals).Level;
							}
							statValue /= Vehicle.AllCapablePawns.Count;
							int countByFishingSkill = Mathf.CeilToInt((statValue / 10) * VehicleMod.settings.main.fishingMultiplier);
							if (countByFishingSkill <= 0)
							{
								countByFishingSkill = 1;
							}
							Thing fish = ThingMaker.MakeThing(fishDef);
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