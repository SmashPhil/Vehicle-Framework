using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using SmashTools.Performance;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Vehicles;

[PublicAPI]
public abstract class JobDriverLoadVehicleBase : JobDriver
{
  private const int WaitTicks = 25;
  private const int MaxTicksGatherItems = 7500;
  private const int LoopBackstop = 500;

  internal static readonly AccessTools.FieldRef<Pawn_InventoryTracker, List<Thing>> UnpackedCaravanItems =
    AccessTools.FieldRefAccess<List<Thing>>(typeof(Pawn_InventoryTracker), "unpackedCaravanItems");

  private int toilLoops;
  private int pickedUpFirstItemTicks = -1;
  private PrepareCaravanGatherState gatherState;

  [CanBeNull]
  public Thing ToHaul => job.GetTarget(TargetIndex.A).Thing;

  protected Pawn Carrier => job.GetTarget(TargetIndex.B).Thing as Pawn;

  protected virtual bool ShouldHaulItems =>
    MassUtility.IsOverEncumbered(pawn) && !pawn.inventory.HasAnyUnpackedCaravanItems;

  protected abstract bool ShouldFailJob();

  protected abstract int CountLeftToTransfer();

  protected abstract Thing FindThingToHaul();

  protected abstract bool IsUsableCarrier(Pawn carrier, bool allowColonists = true);

  protected abstract bool HasDuplicateOpportunity(Thing thing);

  /// <summary>
  /// Event when thing is added to vehicle's inventory
  /// </summary>
  /// <param name="thing"></param>
  protected virtual void OnThingAddedToInventory(Thing thing)
  {
  }

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    if (!pawn.Reserve(ToHaul, job, errorOnFailed: errorOnFailed))
    {
      return false;
    }
    pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job);
    return true;
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    if (gatherState == PrepareCaravanGatherState.Unset)
    {
      gatherState = ShouldHaulItems ?
        PrepareCaravanGatherState.Carry :
        PrepareCaravanGatherState.Haul;
    }
    AddFinishAction(DumpAllUnpackedItems);
    if (gatherState == PrepareCaravanGatherState.Carry)
    {
      return MakeNewToilsCarry();
    }
    return MakeNewToilsHaulInInventory();
  }

  private IEnumerable<Toil> MakeNewToilsCarry()
  {
    this.FailOn(ShouldFailJob);
    Toil reserve = Toils_Reserve.Reserve(TargetIndex.A).FailOnDespawnedOrNull(TargetIndex.A);
    yield return reserve;
    bool inInventory = HaulAIUtility.IsInHaulableInventory(ToHaul);
    yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, canGotoSpawnedParent: inInventory);
    yield return DetermineNumToHaul();
    yield return Toils_Haul.StartCarryThing(TargetIndex.A, subtractNumTakenFromJobCount: true,
      canTakeFromInventory: inInventory);
    Toil carryToil = StartedCarryingThing();
    if (carryToil != null)
    {
      yield return carryToil;
    }
    yield return Toils_Haul.CheckForGetOpportunityDuplicate(reserve,
      haulableInd: TargetIndex.A,
      storeCellInd: TargetIndex.None,
      takeFromValidStorage: true,
      HasDuplicateOpportunity);
    Toil findCarrier = FindCarrier();
    yield return findCarrier;
    yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
     .JumpIf(() => !IsUsableCarrier(Carrier), findCarrier);
    yield return Toils_General.Wait(WaitTicks)
     .JumpIf(() => !IsUsableCarrier(Carrier), findCarrier)
     .WithProgressBarToilDelay(TargetIndex.B);
    yield return PlaceTargetInCarrierInventory();
  }

  private IEnumerable<Toil> MakeNewToilsHaulInInventory()
  {
    this.FailOn(ShouldFailJob);
    bool inInventory = HaulAIUtility.IsInHaulableInventory(ToHaul);
    Toil reserve = Toils_Reserve.Reserve(TargetIndex.A).FailOnDestroyedOrNull(TargetIndex.A);
    Toil findCarrier = FindCarrier();
    yield return reserve;
    yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch, inInventory)
     .JumpIf(IsFinishedCollectingItems, findCarrier);
    yield return DetermineNumToHaul(findCarrier);
    yield return Toils_Haul.StartCarryThing(TargetIndex.A, subtractNumTakenFromJobCount: true,
      canTakeFromInventory: inInventory);
    Toil carryToil = StartedCarryingThing();
    if (carryToil != null)
    {
      yield return carryToil;
    }

    Toil pickUpHauledItem = HaulCaravanItemInInventory(reserve);
    // For non-caravan jobs, TargetIndex.B will always be the vehicle. Use the
    // hauled thing for the progress bar so it appears right on top of the pawn.
    yield return Toils_General.Wait(WaitTicks).WithProgressBarToilDelay(TargetIndex.A);
    yield return pickUpHauledItem;
    yield return findCarrier;
    yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).JumpIf(
      () => !IsUsableCarrier(Carrier), findCarrier);
    yield return Toils_General.Wait(WaitTicks).JumpIf(() => !IsUsableCarrier(Carrier), findCarrier)
     .WithProgressBarToilDelay(TargetIndex.B);
    yield return AddHauledItemsToCarrier(findCarrier);
  }

  private bool IsFinishedCollectingItems()
  {
    if (pawn.carryTracker.CarriedThing is Pawn)
      return true;

    return MassUtility.IsOverEncumbered(pawn) || pickedUpFirstItemTicks > -1 &&
      Find.TickManager.TicksGame > pickedUpFirstItemTicks + MaxTicksGatherItems;
  }

  private Toil HaulCaravanItemInInventory(Toil reserve)
  {
    Toil toil = ToilMaker.MakeToil();
    toil.initAction = delegate
    {
      if (pickedUpFirstItemTicks == -1)
      {
        pickedUpFirstItemTicks = Find.TickManager.TicksGame;
      }
      pawn.records?.Increment(RecordDefOf.ThingsHauled);
      if (pawn.carryTracker.CarriedThing is not Pawn)
      {
        pawn.inventory.AddHauledCaravanItem(pawn.carryTracker.CarriedThing);
      }
      if (!IsFinishedCollectingItems())
      {
        SetNewHaulTargetAndJumpToReserve(reserve);
      }
    };
    return toil;
  }

  private Toil AddHauledItemsToCarrier(Toil findCarrier)
  {
    Toil toil = ToilMaker.MakeToil();
    toil.initAction = delegate
    {
      if (Carrier == pawn)
      {
        pawn.inventory.ClearHaulingCaravanCache();
        return;
      }
      TransferCaravanItemsToCarrier(pawn, Carrier);
      if (pawn.inventory.HasAnyUnpackedCaravanItems && CheckToilLoopBackstop())
      {
        pawn.jobs.curDriver.JumpToToil(findCarrier);
      }
    };
    return toil;

    void TransferCaravanItemsToCarrier(Pawn pawn, Pawn carrier)
    {
      VehiclePawn vehicle = Carrier as VehiclePawn;
      if (vehicle != null && ToHaul is Pawn hauledPawn)
      {
        vehicle.TryAddPawn(hauledPawn);
        return;
      }
      List<Thing> unpackedThings = UnpackedCaravanItems(pawn.inventory);
      for (int i = unpackedThings.Count - 1; i >= 0; i--)
      {
        if (MassUtility.IsOverEncumbered(carrier))
          return;

        Thing thing = unpackedThings[i];
        OnThingAddedToInventory(thing);
        if (vehicle != null)
        {
          AddItemToVehicle(vehicle, thing);
        }
        else
        {
          pawn.inventory.innerContainer.TryTransferToContainer(thing, carrier.inventory.innerContainer,
            thing.stackCount);
        }
        unpackedThings.Remove(thing);
      }
    }
  }

  private void SetNewHaulTargetAndJumpToReserve(Toil reserve)
  {
    if (!CheckToilLoopBackstop())
      return;

    Thing thing = FindThingToHaul();
    if (thing != null)
    {
      job.SetTarget(TargetIndex.A, thing);
      pawn.jobs.curDriver.JumpToToil(reserve);
    }
  }

  private bool CheckToilLoopBackstop()
  {
    if (++toilLoops <= LoopBackstop)
      return true;

    Log.Error($"Prepare caravan gather items job for pawn {pawn.Label} looped through toils too many times.");
    EndJobWith(JobCondition.Errored);
    return false;
  }

  private Toil DetermineNumToHaul(Toil findCarrier = null)
  {
    Toil toil = ToilMaker.MakeToil();
    toil.initAction = delegate
    {
      int count = CountLeftToTransfer();
      if (pawn.carryTracker.CarriedThing != null)
      {
        count -= pawn.carryTracker.CarriedThing.stackCount;
      }
      if (count > 0)
      {
        job.count = count;
        return;
      }
      if (findCarrier == null || !pawn.inventory.HasAnyUnpackedCaravanItems)
      {
        pawn.jobs.EndCurrentJob(JobCondition.Succeeded);
        return;
      }
      pawn.jobs.curDriver.JumpToToil(findCarrier);
    };
    toil.defaultCompleteMode = ToilCompleteMode.Instant;
    toil.atomicWithPrevious = true;
    return toil;
  }

  protected virtual Toil StartedCarryingThing()
  {
    return null;
  }

  /// <summary>
  /// Tries to find a valid carrier if one is not already set.
  /// </summary>
  /// <remarks>
  /// If job is not assigned with a <see cref="LordJob"/>, only the pre-assigned <see cref="Carrier"/>
  /// will be valid. If <see cref="Carrier"/> is null, the job will fail as <see cref="JobCondition.Incompletable"/>
  /// </remarks>
  private Toil FindCarrier()
  {
    return new Toil
    {
      initAction = delegate
      {
        if (job.GetTarget(TargetIndex.B).Pawn != null)
          return;

        if (!TryGetBestCarrier(out Pawn carrier))
        {
          EndJobWith(JobCondition.Incompletable);
          return;
        }
        job.SetTarget(TargetIndex.B, carrier);
      }
    };

    bool TryGetBestCarrier(out Pawn carrier)
    {
      carrier = FindBestCarrier();
      carrier ??= FindBestBackupCarrier(onlyAnimals: true);
      if (carrier != null)
        return true;

      if (job.lord == null)
        return false;

      bool sameLordJob = pawn.GetLord() == job.lord;
      if (sameLordJob && !MassUtility.IsOverEncumbered(pawn))
      {
        carrier = pawn;
        return true;
      }

      carrier = FindBestBackupCarrier(onlyAnimals: false);
      if (carrier != null)
        return true;

      if (sameLordJob)
      {
        carrier = pawn;
        return true;
      }

      List<Pawn> allUsableCarriers =
        job.lord.ownedPawns.Where(carrier => IsUsableCarrier(carrier, allowColonists: true)).ToList();
      carrier = allUsableCarriers.RandomElementWithFallback();
      return carrier != null;
    }
  }

  private Toil PlaceTargetInCarrierInventory()
  {
    Toil toil = ToilMaker.MakeToil();
    toil.initAction = delegate
    {
      if (ToHaul is null || ToHaul.stackCount == 0)
      {
        pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
        return;
      }

      Pawn_CarryTracker carryTracker = pawn.carryTracker;
      Thing carriedThing = carryTracker.CarriedThing;
      Assert.AreEqual(ToHaul, carriedThing);
      if (carryTracker.innerContainer.Count == 0)
      {
        carryTracker.pawn.Drawer.renderer.SetAllGraphicsDirty();
      }
      OnThingAddedToInventory(carriedThing);

      Thing addedThing;
      if (Carrier is VehiclePawn vehicle)
      {
        if (carriedThing is Pawn hauledPawn)
        {
          vehicle.TryAddPawn(hauledPawn);
          return;
        }
        addedThing = AddItemToVehicle(vehicle, carriedThing) ? carriedThing : null;
      }
      else
      {
        carryTracker.innerContainer.TryTransferToContainer(carriedThing, Carrier.inventory.innerContainer,
          carriedThing.stackCount, out addedThing);
      }
      if (addedThing?.TryGetComp<CompForbiddable>() is { } compForbiddable)
      {
        compForbiddable.Forbidden = false;
      }
    };
    return toil;
  }

  protected virtual bool AddItemToVehicle(VehiclePawn vehicle, Thing thing)
  {
    return vehicle.AddOrTransfer(thing, thing.stackCount) > 0;
  }

  private void DumpAllUnpackedItems(JobCondition condition)
  {
    if (pawn.inventory is not { HasAnyUnpackedCaravanItems: true })
      return;
    pawn.inventory.DropAllPackingCaravanThings();
  }

  // Same logic as base game, copied for matching behavior
  private VehiclePawn FindBestCarrier()
  {
    if (job.lord is null)
      return null;

    float highestScore = 0f;
    VehiclePawn carrier = null;
    foreach (Pawn ownedPawn in job.lord.ownedPawns)
    {
      if (ownedPawn != pawn && ownedPawn is VehiclePawn vehicle && IsUsableCarrier(vehicle))
      {
        float carrierScore = GetCarrierScore(ownedPawn, pawn);
        if (carrier == null || carrierScore > highestScore)
        {
          carrier = vehicle;
          highestScore = carrierScore;
        }
      }
    }
    return carrier;
  }

  // Same logic as base game, copied for matching behavior
  private Pawn FindBestBackupCarrier(bool onlyAnimals)
  {
    if (job.lord is null)
      return null;

    float highestScore = 0f;
    Pawn carrier = null;
    foreach (Pawn ownedPawn in job.lord.ownedPawns)
    {
      if (ownedPawn != pawn && (!onlyAnimals || ownedPawn.RaceProps.Animal) &&
        IsUsableCarrier(ownedPawn, allowColonists: false))
      {
        float carrierScore = GetCarrierScore(ownedPawn, pawn);
        if (carrier == null || carrierScore > highestScore)
        {
          carrier = ownedPawn;
          highestScore = carrierScore;
        }
      }
    }
    return carrier;
  }

  public override void ExposeData()
  {
    base.ExposeData();
    Scribe_Values.Look(ref pickedUpFirstItemTicks, nameof(pickedUpFirstItemTicks), defaultValue: -1);
    Scribe_Values.Look(ref toilLoops, nameof(toilLoops));
    Scribe_Values.Look(ref gatherState, nameof(gatherState));
  }

  private static float GetCarrierScore(Pawn carrier, Pawn hauler)
  {
    float availableSpacePct = 1f - MassUtility.EncumbrancePercent(carrier);
    return availableSpacePct - (carrier.Position - hauler.Position).LengthHorizontal / 10f * 0.2f;
  }

  private static int CountBeingCarried(Pawn pawn, ISharedJobSearch searcher)
  {
    int count = 0;
    foreach (Thing thing in UnpackedCaravanItems.Invoke(pawn.inventory))
    {
      count += searcher.IsMatchingThing(thing) ? thing.stackCount : 0;
    }
    return count;
  }

  public static class Search
  {
    [Profile]
    public static Thing FindNearestThing(Pawn pawn, Predicate<Thing> extraValidator)
    {
      Thing result = ClosestHaulable(pawn, ThingRequestGroup.Pawn, validator: ValidThing);
      result ??= ClosestHaulable(pawn, ThingRequestGroup.HaulableEver, validator: ValidThing);
      return result;

      bool ValidThing(Thing thing)
      {
        return extraValidator(thing) && pawn.CanReserve(thing) &&
          !thing.IsForbidden(pawn.Faction);
      }

      static Thing ClosestHaulable(Pawn pawn, ThingRequestGroup thingRequestGroup, Predicate<Thing> validator)
      {
        return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(thingRequestGroup),
          PathEndMode.Touch, TraverseParms.For(pawn), validator: validator);
      }
    }

    [Profile]
    public static int CountAlreadyBeingPacked(Pawn pawn, ISharedJobSearch jobSearcher)
    {
      int mechCount = 0;
      if (ModsConfig.BiotechActive)
      {
        mechCount = HauledByOthers(pawn, jobSearcher, pawn.Map.mapPawns.SpawnedColonyMechs);
      }
      int slaveCount = 0;
      if (ModsConfig.IdeologyActive)
      {
        slaveCount = HauledByOthers(pawn, jobSearcher, pawn.Map.mapPawns.SlavesOfColonySpawned);
      }
      int hauledByOthers = HauledByOthers(pawn, jobSearcher, pawn.Map.mapPawns.FreeColonistsSpawned);
      int hauledBySelf = 0;
      foreach (Thing thing in UnpackedCaravanItems.Invoke(pawn.inventory))
      {
        hauledBySelf += thing.def == jobSearcher.ThingDef ? thing.stackCount : 0;
      }
      return mechCount + slaveCount + hauledBySelf + hauledByOthers;
    }

    private static int HauledByOthers(Pawn pawn, ISharedJobSearch jobSearcher, List<Pawn> pawns)
    {
      int count = 0;
      foreach (Pawn otherPawn in pawns)
      {
        count += CountFromJob(pawn, otherPawn, jobSearcher);
      }
      return count;
    }

    private static int CountFromJob(Pawn pawn, Pawn otherPawn, ISharedJobSearch jobSearcher)
    {
      if (pawn == otherPawn)
        return 0;
      if (otherPawn.CurJob == null)
        return 0;
      if (!jobSearcher.ShouldConsiderPawn(otherPawn))
        return 0;
      if (otherPawn.jobs.curDriver is not JobDriverLoadVehicleBase otherDriver)
        return 0;

      int targetCount = otherDriver.ToHaul?.stackCount ?? 0;
      int countInInventory = CountBeingCarried(otherPawn, jobSearcher);
      return targetCount + countInInventory;
    }
  }
}