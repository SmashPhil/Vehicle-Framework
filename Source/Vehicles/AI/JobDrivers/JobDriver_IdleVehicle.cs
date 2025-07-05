using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Vehicles.Compatibility;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class JobDriver_IdleVehicle : JobDriver
{
  protected VehiclePawn Vehicle => TargetA.Thing as VehiclePawn;

  protected int TicksToFish => VehicleMod.settings.main.fishingDelay -
    Mathf.RoundToInt(Vehicle.AverageSkillOfCapablePawns(SkillDefOf.Animals) / 20f *
      VehicleMod.settings.main.fishingDelay) + VehicleMod.settings.main.fishingDelay;

  public override void Notify_PatherFailed()
  {
    // Can't set JobCondition as Errored or ErroredPather otherwise it will force assign a Wait job
    // which really messes with idling behavior.
    EndJobWith(JobCondition.Incompletable);
  }

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    return true;
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    this.FailOnDestroyedOrNull(TargetIndex.A);
    this.FailOn(() => !Vehicle.Spawned);
    int ticksTillFish = int.MaxValue;
    yield return new Toil
    {
      initAction = delegate
      {
        if (Vehicle.IsBoat())
          ticksTillFish = TicksToFish;
        Map.pawnDestinationReservationManager.Reserve(Vehicle, job, Vehicle.Position);
        Vehicle.vehiclePather.StopDead();
      },
      tickAction = delegate
      {
        if (Vehicle.currentlyFishing && Vehicle.CanMoveFinal)
        {
          ticksTillFish--;
          foreach (Pawn boardedPawn in Vehicle.AllPawnsAboard)
          {
            boardedPawn.skills.Learn(SkillDefOf.Animals, VehicleMod.FishingSkillValue);
          }
          if (ticksTillFish <= 0)
          {
            ticksTillFish = TicksToFish;
            ThingDef fishDef = FishingCompatibility.FetchViableFish(Vehicle.Map.Biome,
              Vehicle.Map.terrainGrid.TerrainAt(Vehicle.Position));

            float statValue = 0;
            foreach (Pawn boardedPawn in Vehicle.AllCapablePawns)
            {
              statValue += boardedPawn.skills.GetSkill(SkillDefOf.Animals).Level;
            }
            statValue /= Vehicle.AllCapablePawns.Count;
            int countByFishingSkill =
              Mathf.CeilToInt((statValue / 10) * VehicleMod.settings.main.fishingMultiplier);
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