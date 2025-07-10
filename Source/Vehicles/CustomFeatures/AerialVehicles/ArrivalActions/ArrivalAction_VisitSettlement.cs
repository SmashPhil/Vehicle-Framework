using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Vehicles.World;
using Verse;

namespace Vehicles;

// TODO - can be removed with some cleanup
[PublicAPI]
public class ArrivalAction_VisitSettlement : ArrivalAction_LandToCaravan
{
  public ArrivalAction_VisitSettlement()
  {
  }

  public ArrivalAction_VisitSettlement(VehiclePawn vehicle) : base(vehicle)
  {
  }

  public static FloatMenuAcceptanceReport CanVisit(VehiclePawn vehicle, Settlement settlement)
  {
    if (settlement is null || !settlement.Spawned || !settlement.Visitable)
      return false;
    if (!WorldVehiclePathGrid.Instance.Passable(settlement.Tile, vehicle.VehicleDef))
      return FloatMenuAcceptanceReport.WithFailReason("Impassable".Translate());
    return true;
  }
}