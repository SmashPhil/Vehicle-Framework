using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace Vehicles;

[UsedWithReflection]
public class WorkGiver_PackVehicle : WorkGiver_CarryToVehicle<TransferableOneWay>
{
  protected override bool JobAvailable(Pawn pawn, VehiclePawn vehicle)
  {
    if (vehicle.cargoToLoad.Count == 0)
      return false;

    return !MassUtility.IsOverEncumbered(vehicle);
  }

  protected override List<TransferableOneWay> GetThingsToLoad(VehiclePawn vehicle, Pawn pawn)
  {
    for (int i = vehicle.cargoToLoad.Count - 1; i >= 0; i--)
    {
      if (vehicle.cargoToLoad[i].AnyThing is not Pawn cargoPawn)
        continue;

      // Filter out pawns that no longer have space in the vehicle. If we don't filter them out
      // before the job is assigned, they will attempt to be loaded repeatedly.
      if (!pawn.Spawned || vehicle.GetNextAvailableHandler(cargoPawn) == null)
      {
        vehicle.cargoToLoad.RemoveAt(i);
      }
    }
    return vehicle.cargoToLoad;
  }

  protected override Thing FindThingToPack(VehiclePawn vehicle, Pawn pawn, List<TransferableOneWay> things)
  {
    return JobDriver_LoadVehicle.FindThingToPack(pawn, JobDef, things);
  }
}