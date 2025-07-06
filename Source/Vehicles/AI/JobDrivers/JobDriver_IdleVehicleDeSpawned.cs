using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using Verse;
using Verse.AI;

namespace Vehicles;

[PublicAPI]
public class JobDriver_IdleVehicleDeSpawned : JobDriver
{
  public override bool TryMakePreToilReservations(bool errorOnFailed)
  {
    return true;
  }

  protected override IEnumerable<Toil> MakeNewToils()
  {
    Assert.IsTrue(pawn is VehiclePawn);
    AddEndCondition(() => pawn.Spawned ? JobCondition.Succeeded : JobCondition.Ongoing);
    yield return IdleWhileDespawned();
  }

  private static Toil IdleWhileDespawned()
  {
    Toil toil = ToilMaker.MakeToil();
    toil.defaultCompleteMode = ToilCompleteMode.Never;
    return toil;
  }
}