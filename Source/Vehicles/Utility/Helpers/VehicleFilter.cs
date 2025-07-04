using System.Collections.Generic;
using Verse;

namespace Vehicles;

public static class VehicleFilter
{
  /// <summary>
  /// Removes duplicate pawns for caravans by filtering out pawns already inside a vehicle that is
  /// also within the enumerable.
  /// </summary>
  public static IEnumerable<Pawn> FilterOutPassengers(IEnumerable<Pawn> pawns)
  {
    List<Pawn> pawnsInVehicles = [];
    HashSet<VehiclePawn> vehicles = [];

    foreach (Pawn pawn in pawns)
    {
      if (pawn.InVehicle())
      {
        pawnsInVehicles.Add(pawn);
      }
      else
      {
        if (pawn is VehiclePawn vehicle)
          vehicles.Add(vehicle);
        yield return pawn;
      }
    }

    foreach (Pawn pawn in pawnsInVehicles)
    {
      if (!vehicles.Contains(pawn.GetVehicle()))
        yield return pawn;
    }
  }
}