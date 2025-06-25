using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace Vehicles.World;

public abstract class WITab_AerialVehicle : WITab
{
  protected AerialVehicleInFlight SelAerialVehicle => SelObject as AerialVehicleInFlight;

  protected List<Pawn> Pawns
  {
    get { return SelAerialVehicle.vehicle.AllPawnsAboard; }
  }
}