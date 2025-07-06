using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using RimWorld;
using SmashTools;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class JobDriver_LoadVehicle : JobDriver
{
  private static readonly FieldInfo CountToTransferFieldInfo =
    AccessTools.Field(typeof(TransferableOneWay), "countToTransfer");

  protected virtual string ListerTag => ReservationType.LoadVehicle;

  public virtual Thing Item
  {
    get { return job.GetTarget(TargetIndex.A).Thing; }
  }

  protected virtual VehiclePawn Vehicle
  {
    get { return job.GetTarget(TargetIndex.B).Thing as VehiclePawn; }
  }

  protected virtual bool FailJob()
  {
    return !MapComponentCache<VehicleReservationManager>.GetComponent(Map)
     .VehicleListed(Vehicle, ListerTag);
  }

  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    if (!pawn.Reserve(Item, job))
    {
      return false;
    }
    pawn.ReserveAsManyAsPossible(job.GetTargetQueue(TargetIndex.A), job);
    return true;
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    this.FailOnDestroyedOrNull(TargetIndex.A);
    this.FailOnDestroyedOrNull(TargetIndex.B);
    this.FailOnForbidden(TargetIndex.A);
    this.FailOn(FailJob);
    yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
    yield return Toils_Haul.StartCarryThing(TargetIndex.A);
    yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch)
     .FailOnDespawnedNullOrForbidden(TargetIndex.B);
    yield return Toils_General.Wait(25).WithProgressBarToilDelay(TargetIndex.B);
    yield return GiveAsMuchToVehicleAsPossible();
  }

  protected virtual Toil FindNearestVehicle()
  {
    return new Toil
    {
      initAction = delegate
      {
        Pawn pawn = CaravanHelper.UsableVehicleWithTheMostFreeSpace(this.pawn);
        if (pawn is null)
        {
          this.pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true);
        }
        else
        {
          job.SetTarget(TargetIndex.B, pawn);
        }
      }
    };
  }

  protected virtual Toil GiveAsMuchToVehicleAsPossible()
  {
    return new Toil
    {
      initAction = delegate
      {
        if (Item is null || Item.stackCount == 0)
        {
          pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
        }
        else
        {
          int stackCount = Item.stackCount; //store before transfer for transferable recache

          Vehicle.AddOrTransfer(Item, stackCount);
          TransferableOneWay transferable = GetTransferable(Vehicle.cargoToLoad, Vehicle, Item);
          if (transferable != null)
          {
            int count = transferable.CountToTransfer - stackCount;
            CountToTransferFieldInfo.SetValue(transferable, count);
            if (transferable.CountToTransfer <= 0)
            {
              Vehicle.cargoToLoad.Remove(transferable);
            }
          }
        }
      }
    };
  }

  public static TransferableOneWay GetTransferable(List<TransferableOneWay> transferables,
    VehiclePawn vehicle, Thing thing)
  {
    foreach (TransferableOneWay transferable in transferables)
    {
      foreach (Thing transferableThing in transferable.things)
      {
        if (transferableThing == thing)
          return transferable;
      }
    }
    //Unable to find thing instance, match on def
    foreach (TransferableOneWay transferable in transferables)
    {
      foreach (Thing transferableThing in transferable.things)
      {
        if (transferableThing.def == thing.def)
          return transferable;
      }
    }
    return null;
  }
}