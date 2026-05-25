using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using UnityEngine;
using Vehicles.Compatibility;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class JobDriver_IdleVehicle : JobDriver
{
  private const int FishingTicksMin = 2000;
  private const int FishingTicksMax = 6000;

  // Odyssey is 7500 ticks base, vehicles get ~10% discount for incentive to use boats for fishing
  private const int FishingTicksOdyssey = 6800;

  private static readonly IntRange MoteIntervalRange = new(240, 280);

  protected VehiclePawn Vehicle => TargetA.Thing as VehiclePawn;

  private int TicksToFish
  {
    get
    {
      FishingProperties fishingProps = Vehicle.VehicleDef.fishingProperties;
      int fishingSkill = fishingProps?.animalSkillOverride ?? Vehicle.AverageSkillOfCapablePawns(SkillDefOf.Animals);
      float ticksToFish = ModsConfig.OdysseyActive ?
        Mathf.RoundToInt(FishingTicksMax / pawn.GetStatValue(StatDefOf.FishingSpeed)) :
        Mathf.Lerp(fishingSkill, FishingTicksMin, FishingTicksMax);

      if (fishingProps != null)
      {
        ticksToFish *= fishingProps.fishingTicksModifier;
      }
      return Mathf.CeilToInt(ticksToFish);
    }
  }

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

  private static Vector3 GetRandomFishingPosition(VehiclePawn vehicle, out float angle)
  {
    FishingProperties fishingProps = vehicle.VehicleDef.fishingProperties;

    IntVec2 offset;
    if (fishingProps?.fishingCells is { Count: > 0 })
    {
      offset = fishingProps.fishingCells.RandomElement();
    }
    else
    {
      int horizontalOffset = Mathf.FloorToInt(vehicle.VehicleDef.Size.x / 2f) + 1;
      offset = new IntVec2(Rand.Bool ? horizontalOffset : -horizontalOffset, 0);
    }
    float thresholdX = vehicle.VehicleDef.Size.x / 2f;
    float thresholdY = vehicle.VehicleDef.Size.z / 2f;
    angle = vehicle.FullRotation.AsAngle;
    if (Mathf.Abs(offset.z) > thresholdY && offset.z < 0)
    {
      angle *= -1;
    }
    if (Mathf.Abs(offset.x) > thresholdX)
    {
      angle += 90 * Mathf.Sign(offset.x);
    }
    angle = angle.ClampAngle();

    Vector2 position = offset.ToVector2Shifted().RotatePointClockwise(vehicle.FullRotation.AsAngle);
    return vehicle.DrawPos + position.ToVector3();
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    this.FailOnDestroyedOrNull(TargetIndex.A);
    this.FailOn(() => !Vehicle.Spawned);
    int ticksTillFish = int.MaxValue;
    int ticksToThrowMote = MoteIntervalRange.RandomInRange;
    yield return new Toil
    {
      initAction = delegate
      {
        if (Vehicle.IsBoat())
          ticksTillFish = TicksToFish;
        Map.pawnDestinationReservationManager.Reserve(Vehicle, job, Vehicle.Position);
        Vehicle.vehiclePather.StopDead();
      },
      tickAction = TickFishAction,
      defaultCompleteMode = ToilCompleteMode.Never
    };
    yield break;

    void TickFishAction()
    {
      if (Vehicle.fishingTracker is not { IsFishing: true })
        return;

      ticksTillFish--;
      ticksToThrowMote--;
      if (ticksToThrowMote <= 0)
      {
        const float Velocity = 0.55f;
        const float RotateRateMin = 2;
        const float RotateRateMax = 6;

        ticksToThrowMote = MoteIntervalRange.RandomInRange;
        Vector3 position = GetRandomFishingPosition(Vehicle, out float angle);
        position.y = AltitudeLayer.MoteOverheadLow.AltitudeFor();
        MoteThrown mote = (MoteThrown)ThingMaker.MakeThing(ThingDefOf_VehicleMotes.MoteFishingNet);
        mote.exactPosition = position;
        mote.Scale = 1f;
        mote.rotationRate = Rand.Range(RotateRateMin, RotateRateMax) * (Rand.Bool ? 1 : -1);
        mote.SetVelocity(angle, Velocity);
        MoteGenerator.ThrowMote(position.ToIntVec3(), Vehicle.Map, mote);
      }

      foreach (Pawn boardedPawn in Vehicle.AllPawnsAboard)
      {
        boardedPawn.skills?.Learn(SkillDefOf.Animals, VehicleMod.FishingSkillValue);
      }
      if (ticksTillFish <= 0)
      {
        ticksTillFish = TicksToFish;
        IntVec3 fishCell = Vehicle.Position;
        List<Thing> catchesFor =
          FishingCompatibility.GetCatchesFor(Vehicle, fishCell, Vehicle.Map.Biome, out bool rare);
        if (catchesFor.Count == 0)
          return;

        if (rare)
        {
          Vehicle.Map.waterBodyTracker.lastRareCatchTick = Find.TickManager.TicksGame;
          Find.LetterStack.ReceiveLetter("LetterLabelRareCatch".Translate(),
            $"{"LetterTextRareCatch".Translate(pawn.Named("PAWN"))}:\n{catchesFor.Select(thing => thing.LabelCap).ToLineList("  - ")}",
            textLetterDef: LetterDefOf.PositiveEvent, lookTargets: catchesFor);
        }
        else if (ModsConfig.OdysseyActive)
        {
          int amount = catchesFor.Sum(thing => thing.stackCount);
          Vehicle.Map.waterBodyTracker.Notify_Fished(fishCell, amount);
          Find.HistoryEventsManager.RecordEvent(
            new HistoryEvent(HistoryEventDefOf.SlaughteredFish, pawn.Named(HistoryEventArgsNames.Doer)));
        }

        foreach (Thing thing in catchesFor)
        {
          Vehicle.AddOrTransfer(thing);
        }
      }
    }
  }
}