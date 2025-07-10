using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

// ReSharper disable once InconsistentNaming
// It's cleaner to just stick with RimWorld's naming convention when deriving from their types.
public abstract class WITab_AerialVehicle : WITab
{
  protected AerialVehicleInFlight SelAerialVehicle => SelObject as AerialVehicleInFlight;

  protected List<Pawn> Pawns
  {
    get { return SelAerialVehicle.Vehicle.AllPawnsAboard; }
  }
}