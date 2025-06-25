using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

public static class Ext_Caravan
{
  /// <summary>
  /// Caravan contains one or more Vehicles
  /// </summary>
  public static bool HasVehicle(this Caravan caravan)
  {
    return (caravan is VehicleCaravan vehicleCaravan &&
        vehicleCaravan.pawns.InnerListForReading.HasVehicle()) ||
      (Dialog_FormVehicleCaravan.CurrentFormingCaravan != null &&
        TransferableUtility
         .GetPawnsFromTransferables(Dialog_FormVehicleCaravan.CurrentFormingCaravan.transferables)
         .HasVehicle());
  }

  /// <summary>
  /// Caravan contains one or more Boats
  /// </summary>
  public static bool HasBoat(this Caravan caravan)
  {
    return (caravan is VehicleCaravan vehicleCaravan &&
        vehicleCaravan.pawns.InnerListForReading.HasBoat()) ||
      (Dialog_FormVehicleCaravan.CurrentFormingCaravan != null &&
        TransferableUtility
         .GetPawnsFromTransferables(Dialog_FormVehicleCaravan.CurrentFormingCaravan.transferables)
         .HasBoat());
  }

  /// <summary>
  /// Get all unique Vehicles in Caravan <paramref name="caravan"/>
  /// </summary>
  /// <returns>New <see cref="HashSet{T}"/> with unique VehicleDefs in <paramref name="caravan"/></returns>
  public static HashSet<VehicleDef> UniqueVehicleDefsInCaravan(this Caravan caravan)
  {
    HashSet<VehicleDef> vehicleSet = [];
    foreach (Pawn pawn in caravan.PawnsListForReading)
    {
      if (pawn is VehiclePawn vehicle)
        vehicleSet.Add(vehicle.VehicleDef);
    }
    return vehicleSet;
  }

  /// <summary>
  /// Validate if <paramref name="vehicle"/> is able to join <paramref name="vehicleCaravan"/> without causing caravan to not be able to path on world map with current path settings
  /// </summary>
  /// <param name="vehicleCaravan"></param>
  /// <param name="vehicle"></param>
  public static bool ViableForCaravan(this VehicleCaravan vehicleCaravan, VehiclePawn vehicle)
  {
    foreach (VehiclePawn caravanVehicle in vehicleCaravan.VehiclesListForReading)
    {
      if (!GridOwners.World.MatchingReachability(caravanVehicle.VehicleDef, vehicle.VehicleDef))
      {
        return false;
      }
    }

    return true;
  }

  /// <summary>
  /// Get all pawns from Caravan inside vehicles
  /// </summary>
  /// <param name="caravan"></param>
  public static List<Pawn> GrabPawnsFromVehicleCaravanSilentFail(this Caravan caravan)
  {
    if (caravan is null || !caravan.HasVehicle())
      return null;

    List<Pawn> vehicles = [];
    foreach (Pawn p in caravan.PawnsListForReading)
    {
      if (p is VehiclePawn vehicle)
      {
        vehicles.AddRange(vehicle.AllPawnsAboard);
      }
      else
      {
        vehicles.Add(p);
      }
    }
    return vehicles;
  }

  public static void TransferPawnOrItem(this Caravan caravan, ThingOwner owner, Thing thing)
  {
    if (thing is Pawn)
    {
      owner.TryTransferToContainer(thing, caravan.pawns, canMergeWithExistingStacks: false);
    }
    else
    {
      Pawn giveToPawn =
        CaravanInventoryUtility.FindPawnToMoveInventoryTo(thing, caravan.PawnsListForReading,
          null);
      if (giveToPawn == null || !giveToPawn.inventory.innerContainer.TryAddOrTransfer(thing))
      {
        Log.Error($"Failed to give item {thing} to caravan {caravan}; item was lost.");
        thing.Destroy();
      }
    }
  }
}